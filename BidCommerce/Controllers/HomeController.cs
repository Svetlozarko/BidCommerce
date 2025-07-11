using BidCommerce.Data;
using BidCommerce.Interfaces;
using BidCommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BidCommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICategoryCountCacheService _cache;
        private readonly BidDb _context;

        public HomeController(ILogger<HomeController> logger, ICategoryCountCacheService cache, BidDb context)
        {
            _logger = logger;
            _cache = cache;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
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

            return View(categories);
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
