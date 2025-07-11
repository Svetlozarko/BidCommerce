using BidCommerce.Data;
using BidCommerce.Interfaces;
using BidCommerce.Models;
using BidCommerce.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;


namespace BidCommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BidDb _context;
        private readonly ICategoryCountCacheService _cache;
        public HomeController(ILogger<HomeController> logger, BidDb context, ICategoryCountCacheService cache)
        {
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            // Get categories with caching
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            foreach (var category in categories)
            {
                // Try to get from Redis
                var count = await _cache.GetCategoryCountAsync(category.Name);
                if (!count.HasValue)
                {
                    // Fallback: count from DB
                    count = await _context.Products.CountAsync(p => p.CategoryId == category.CategoryId);
                    await _cache.SetCategoryCountAsync(category.Name, count.Value, TimeSpan.FromMinutes(1));
                }
                category.ItemCount = count.Value;
            }

            viewModel.Categories = categories;

            // Get recent products (last 8 products added)
            viewModel.RecentProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
