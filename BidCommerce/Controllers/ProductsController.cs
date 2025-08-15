using System;
using System.Collections.Generic;
using System.IO; // for Path and FileStream
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.ViewModels;
using BidCommerce.Services;
using StackExchange.Redis;
using RateLimiterLib;
using RateLimiterLib.Enums;
using BidCommerce.Interfaces;

namespace BidCommerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BidDb _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDatabase _redis;
        private readonly ISearchableTextRedis _searchTextRedisService;
        private readonly IConfiguration _configuration;

        public ProductsController(BidDb context, UserManager<ApplicationUser> userManager, IConnectionMultiplexer redis, ISearchableTextRedis searchableTextRedis, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _redis = redis.GetDatabase();
            _searchTextRedisService = searchableTextRedis;
            _configuration = configuration; 
        }

        [Authorize]
        public async Task<IActionResult> Index(
          int? categoryId,
          string? category,
          decimal? minPrice,
          decimal? maxPrice,
          string? sortBy,
          string? listingType)
        {
            var expiredProducts = await _context.Products
                .Include(p => p.Status)
                .Where(p => p.IsBiddable && p.Status.Name == "Active" && p.BidEndTime <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredProducts.Any())
            {
                var expiredStatus = await _context.ProductsStatus.FirstOrDefaultAsync(s => s.Name == "Expired");

                foreach (var product in expiredProducts)
                {
                    product.Status = expiredStatus!;
                }

                await _context.SaveChangesAsync();
            }

            var query = _context.Products
      .Include(p => p.Category)
      .Include(p => p.Owner)
      .Include(p => p.Status)     
      .AsQueryable();

            query = query.Where(p => p.Status.Name == "Active");



            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category.Name == category);

            if (minPrice.HasValue)
                query = query.Where(p => (p.BuyNowPrice ?? p.StartingPrice) >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => (p.BuyNowPrice ?? p.StartingPrice) <= maxPrice.Value);

            query = sortBy switch
            {
                "price-low" => query.OrderBy(p => p.BuyNowPrice ?? p.StartingPrice),
                "price-high" => query.OrderByDescending(p => p.BuyNowPrice ?? p.StartingPrice),
                "ending-soon" => query.OrderBy(p => p.BidEndTime),
                _ => query.OrderByDescending(p => p.CreatedAt),
            };

            var products = await query.ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            var bidCounts = new Dictionary<int, int>();
            foreach (var product in products)
            {
                var key = $"product:{product.Id}:bids";
                var count = await _redis.SortedSetLengthAsync(key);
                bidCounts[product.Id] = (int)count;
            }

            ViewBag.BidCounts = bidCounts;

            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                Categories = categories,
                SelectedCategoryId = categoryId,
                SelectedCategoryName = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy ?? "newest",
                ListingType = listingType
            };

            return View(viewModel);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return RedirectToAction(nameof(Index));

            // Normalize the query
            var loweredQuery = query.ToLowerInvariant();

            // Get all Redis keys for searchable text
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: "product:*").ToArray();

            var matchingIds = new List<int>();

            foreach (var key in keys)
            {
                // Only look for keys that store searchable text, not bids or other data
                if (key.ToString().Count(c => c == ':') == 1)
                {
                    var text = await _redis.StringGetAsync(key);
                    if (!text.IsNullOrEmpty && text.ToString().ToLowerInvariant().Contains(loweredQuery))
                    {
                        if (int.TryParse(key.ToString().Split(':')[1], out int productId))
                        {
                            matchingIds.Add(productId);
                        }
                    }
                }
            }

            if (!matchingIds.Any())
            {
                ViewBag.Message = "No products found matching your search.";
                return View("Index", new ProductIndexViewModel
                {
                    Products = new List<Product>(),
                    Categories = await _context.Categories.ToListAsync()
                });
            }

            // Fetch matching products from DB
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .Include(p => p.Status)
                .Where(p => matchingIds.Contains(p.Id) && p.Status.Name == "Active")
                .ToListAsync();

            var bidCounts = new Dictionary<int, int>();
            foreach (var product in products)
            {
                var bidKey = $"product:{product.Id}:bids";
                var count = await _redis.SortedSetLengthAsync(bidKey);
                bidCounts[product.Id] = (int)count;
            }

            ViewBag.BidCounts = bidCounts;

            var vm = new ProductIndexViewModel
            {
                Products = products,
                Categories = await _context.Categories.ToListAsync(),
                SortBy = "search",
            };

            return View("Index", vm);
        }

        [Authorize]
        [RateLimit]
        public async Task<IActionResult> Details(int? id, [FromServices] BidCacheService bidCacheService)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Owner)
                .Include(p => p.Category)
                .Include(p => p.Images) // This is the key addition - loads all ProductImage entities
                .Include(p => p.Status)
                .Include(p => p.Condition)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // Increment view count
            product.Views++;
            await _context.SaveChangesAsync();

            // Get recent bids from Redis
            var redisBids = await bidCacheService.GetRecentBidsAsync(product.Id);
            ViewBag.RedisBids = redisBids.Select(b => new BidCacheService.BidDto
            {
                BidderId = b.BidderId,
                Amount = b.Amount,
                PlacedAt = b.PlacedAt
            }).ToList();

            // Optional: Add debugging to see how many images were loaded
            Console.WriteLine($"Product {id} loaded with {product.Images?.Count ?? 0} images");

            // Optional: Log the image URLs for debugging
            if (product.Images != null && product.Images.Any())
            {
                foreach (var image in product.Images)
                {
                    Console.WriteLine($"Image URL: {image.ImageUrl}");
                }
            }

            return View(product);
        }


        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStripeAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            // Explicitly create client with your key from config
            var client = new Stripe.StripeClient(_configuration["Stripe:SecretKey"]);
            var accountService = new Stripe.AccountService(client);
            var accountLinkService = new Stripe.AccountLinkService(client);

            if (string.IsNullOrEmpty(user.StripeAccountId))
            {
                var accountOptions = new Stripe.AccountCreateOptions
                {
                    Type = "express",
                    Email = user.Email
                };
                var account = await accountService.CreateAsync(accountOptions);

                user.StripeAccountId = account.Id;
                user.StripeAccountConnected = true;
                await _context.SaveChangesAsync();
            }

            var accountLinkOptions = new Stripe.AccountLinkCreateOptions
            {
                Account = user.StripeAccountId,
                RefreshUrl = Url.Action("OnboardingRefresh", "SellerOnboarding", null, Request.Scheme),
                ReturnUrl = Url.Action("OnboardingReturn", "SellerOnboarding", null, Request.Scheme),
                Type = "account_onboarding"
            };

            var accountLink = await accountLinkService.CreateAsync(accountLinkOptions);

            return Json(new { success = true, url = accountLink.Url });
        }


        [Authorize]
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasStripeAccount = await CheckUserHasStripeAccount(userId);

            if (!hasStripeAccount)
            {
                return RedirectToAction("Index", "SellerOnboarding");
            }
            var categories = _context.Categories.ToList();

            var vm = new ProductCreateViewModel
            {
                Categories = categories,
                        Condition = _context.ProductsCondition.ToList()
            };

            return View(vm);
        }
        private async Task<bool> CheckUserHasStripeAccount(string userId)
        {
            // Assuming Identity is set up and ApplicationUser contains HasStripeAccount
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.StripeAccountConnected })
                .FirstOrDefaultAsync();

            return user?.StripeAccountConnected ?? false;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(ProductCreateViewModel vm)
        {
            // Force save as draft
            bool saveAsDraft = true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Minimum check: at least one field must be entered
            if (string.IsNullOrWhiteSpace(vm.Product.Title) &&
                string.IsNullOrWhiteSpace(vm.Product.Description) &&
                vm.Product.StartingPrice == null &&
                (vm.ImageFiles == null || !vm.ImageFiles.Any()))
            {
                ModelState.AddModelError("", "Please enter at least one field before saving as draft.");
                vm.Categories = _context.Categories.ToList();
                return View("Create", vm); // Show the create view with errors
            }

            var product = vm.Product;
            product.OwnerId = userId;
            product.CreatedAt = DateTime.UtcNow;

            // Assign draft status
            product.Status = await _context.ProductsStatus
                .FirstOrDefaultAsync(s => s.Name == "Draft");

            if (product.IsBiddable && product.StartingPrice.HasValue)
            {
                product.CurrentBid = product.StartingPrice.Value;
            }

            // Handle images
            if (vm.ImageFiles != null && vm.ImageFiles.Any())
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                foreach (var file in vm.ImageFiles)
                {
                    if (file.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        product.Images.Add(new ProductImage { ImageUrl = "/images/products/" + fileName });
                    }
                }

                product.ImageUrl = product.Images.FirstOrDefault()?.ImageUrl;
            }

            // Avoid EF circular binding
            product.Category = null;

            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error saving draft: " + ex.Message);
                vm.Categories = _context.Categories.ToList();
                return View("Create", vm);
            }

            TempData["SuccessMessage"] = "Draft saved successfully!";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
[Authorize]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(ProductCreateViewModel vm, bool saveAsDraft = false)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Unauthorized();

    // Skip strict validation if saving as draft
    if (!saveAsDraft && !ModelState.IsValid)
    {
        vm.Categories = _context.Categories.ToList();
        return View(vm);
    }

    // Minimum check: at least one field must be entered
    if (saveAsDraft && string.IsNullOrWhiteSpace(vm.Product.Title) &&
                      string.IsNullOrWhiteSpace(vm.Product.Description) &&
                      vm.Product.StartingPrice == null &&
                      vm.ImageFiles.Count == 0)
    {
        ModelState.AddModelError("", "Please enter at least one field before saving as draft.");
        vm.Categories = _context.Categories.ToList();
        return View(vm);
    }

    var product = vm.Product;
    product.OwnerId = userId;
    product.CreatedAt = DateTime.UtcNow;

    // Assign draft or active status
    product.Status = await _context.ProductsStatus
        .FirstOrDefaultAsync(s => s.Name == (saveAsDraft ? "Draft" : "Active"));

    if (product.IsBiddable && product.StartingPrice.HasValue)
    {
        product.CurrentBid = product.StartingPrice.Value;
    }

    // Handle images
    if (vm.ImageFiles != null && vm.ImageFiles.Any())
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        foreach (var file in vm.ImageFiles)
        {
            if (file.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                product.Images.Add(new ProductImage { ImageUrl = "/images/products/" + fileName });
            }
        }

        product.ImageUrl = product.Images.FirstOrDefault()?.ImageUrl;
    }

    // Avoid EF circular binding
    product.Category = null;

    _context.Products.Add(product);
    try
    {

        await _context.SaveChangesAsync();
                if (saveAsDraft==false)
                {
                    string searchableText = $"{product.Title} {product.Description} {product.CategoryId} {product.Condition}";
                    await _searchTextRedisService.SaveProductAsync(product.Id, searchableText);
                }
            }
            catch (Exception ex)
    {
        ModelState.AddModelError("", "Error saving product: " + ex.Message);
                ModelState.AddModelError("", "Redis error: " + ex.Message);
                vm.Categories = _context.Categories.ToList();
        return View(vm);
    }

    return RedirectToAction(nameof(Index));
}


        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var viewModel = new ProductCreateViewModel
            {
                Product = product,
                Categories = await _context.Categories.ToListAsync(),
                Condition = await _context.ProductsCondition.ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateViewModel viewModel)
        {
            if (id != viewModel.Product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(viewModel.Product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.Product.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            viewModel.Categories = await _context.Categories.ToListAsync();
            viewModel.Condition = await _context.ProductsCondition.ToListAsync();

            return View(viewModel);
        }



        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Owner)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
    int id,
    [FromServices] BidCacheService bidCacheService,
    [FromServices] ISearchableTextRedis searchableTextRedis) 
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                await bidCacheService.RemoveBidsAsync(id);
                await searchableTextRedis.DeleteProductAsync(id);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }




        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        public IActionResult IndexWatchlist()
        {
            return View();
        }


        [Authorize]
        public async Task<IActionResult> Watchlist()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var products = await _context.WatchlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Condition) 
                .Select(w => w.Product)
                .ToListAsync();

            return View(products);
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToWatchlist(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var exists = await _context.WatchlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == id);

            if (!exists)
            {
                var item = new WatchlistItem
                {
                    UserId = userId,
                    ProductId = id
                };

                _context.WatchlistItems.Add(item);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromWatchlist(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var item = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == id);

            if (item != null)
            {
                _context.WatchlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }


    }
}
