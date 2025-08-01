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

namespace BidCommerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BidDb _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDatabase _redis;


        public ProductsController(BidDb context, UserManager<ApplicationUser> userManager, IConnectionMultiplexer redis)
        {
            _context = context;
            _userManager = userManager;
            _redis = redis.GetDatabase();

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
    .Include(p => p.Status)      // <-- Add this!
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
        public IActionResult Create()
        {
            var categories = _context.Categories.ToList();

            var vm = new ProductCreateViewModel
            {
                Categories = categories,
                        Condition = _context.ProductsCondition.ToList()
            };

            return View(vm);
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
    }
    catch (Exception ex)
    {
        ModelState.AddModelError("", "Error saving product: " + ex.Message);
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
     [FromServices] BidCacheService bidCacheService) // Inject your Redis service
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Delete associated bids from Redis
                await bidCacheService.RemoveBidsAsync(id); // You’ll implement this method below

                // Delete product from database
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
