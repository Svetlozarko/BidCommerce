using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BidCommerce.Data;
using BidCommerce.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using BidCommerce.ViewModels;

namespace BidCommerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BidDb _context;

        public ProductsController(BidDb context)
        {
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index(int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy, string? listingType)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // Filtering by category
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // Filtering by price range
            if (minPrice.HasValue)
                query = query.Where(p => p.BuyNowPrice >= minPrice.Value || p.StartingPrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BuyNowPrice <= maxPrice.Value || p.StartingPrice <= maxPrice.Value);

            // Sorting
            query = sortBy switch
            {
                "price-low" => query.OrderBy(p => p.BuyNowPrice ?? p.StartingPrice),
                "price-high" => query.OrderByDescending(p => p.BuyNowPrice ?? p.StartingPrice),
                "ending-soon" => query.OrderBy(p => p.BidEndTime),
                _ => query.OrderByDescending(p => p.CreatedAt), // newest by default
            };

            var products = await query.ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                Categories = categories,
                SelectedCategoryId = categoryId,
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
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        // GET: Product/Create
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
            if (userId == null)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                // Reload categories if validation fails
                vm.Categories = _context.Categories.ToList();

                // Log model state errors
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                foreach (var error in errors)
                {
                    Console.WriteLine("Validation error: " + error);
                }

                return View(vm);
            }

            var product = vm.Product;

            // Debug logs for each important field
            Console.WriteLine($"Product Title: {product.Title}");
            Console.WriteLine($"Product Description: {product.Description}");
            Console.WriteLine($"Product CategoryId: {product.CategoryId}");
            Console.WriteLine($"Product StartingPrice: {product.StartingPrice}");
            Console.WriteLine($"Product BuyNowPrice: {product.BuyNowPrice}");
            Console.WriteLine($"Product IsBiddable: {product.IsBiddable}");

            product.OwnerId = userId;
            product.CreatedAt = DateTime.UtcNow;

            // Image upload check
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(product.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await product.ImageFile.CopyToAsync(stream);

                product.ImageUrl = "/images/products/" + fileName;
                Console.WriteLine("Image uploaded: " + product.ImageUrl);
            }
            else
            {
                Console.WriteLine("No image uploaded.");
            }

            // Optional: To avoid EF confusion, nullify navigation props
            product.Category = null;

            _context.Products.Add(product);
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("Product saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving product: " + ex.Message);
                ModelState.AddModelError("", "Error saving product: " + ex.Message);
                vm.Categories = _context.Categories.ToList();
                return View(vm);
            }

            return RedirectToAction("Index");
        }



        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id", product.OwnerId);
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,Title,Description,StartingPrice,IsBiddable,BuyNowPrice,CurrentBid,BidEndTime,ImageUrl,CreatedAt,OwnerId")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

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
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id", product.OwnerId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
