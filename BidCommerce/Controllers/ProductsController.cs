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

namespace BidCommerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BidDb _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Inject UserManager in constructor
        public ProductsController(BidDb context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

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

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Owner)
                .Include(p => p.Category) // probably useful to include category here too
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        [Authorize]
        public IActionResult Create()
        {
            var categories = _context.Categories.ToList();

            var vm = new ProductCreateViewModel
            {
                Categories = categories
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                vm.Categories = _context.Categories.ToList();
                return View(vm);
            }

            var product = vm.Product;
            product.OwnerId = userId;
            product.CreatedAt = DateTime.UtcNow;

            if (vm.Product.IsBiddable && vm.Product.StartingPrice.HasValue)
            {
                product.CurrentBid = vm.Product.StartingPrice.Value;
            }
            // Assign CategoryId explicitly if present in vm.Product
            // (Assuming CategoryId is part of your Product model)
            // product.CategoryId = vm.Product.CategoryId;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await vm.ImageFile.CopyToAsync(stream);

                product.ImageUrl = "/images/products/" + fileName;
            }
            else
            {
                product.ImageUrl = null; // or set default image path
            }

            // Avoid EF navigation conflicts
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

            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", product.OwnerId); // better to show UserName
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,StartingPrice,IsBiddable,BuyNowPrice,CurrentBid,BidEndTime,ImageUrl,CreatedAt,OwnerId,CategoryId")] Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", product.OwnerId);
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            return View(product);
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
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
            // TODO: Fetch the current user's watchlist items and pass them to the view
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
