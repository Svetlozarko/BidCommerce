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
            var categories = await _context.Categories.ToListAsync();

            foreach (var category in categories)
            {
                var cachedCount = await _cache.GetCategoryCountAsync(category.Name);
                if (cachedCount.HasValue)
                {
                    category.ItemCount = cachedCount.Value;
                }
                else
                {
                    // Query the count from your Products table or wherever you store items
                    var count = await _context.Products.CountAsync(p => p.CategoryId == category.CategoryId);

                    // Cache it
                    await _cache.SetCategoryCountAsync(category.Name, count, TimeSpan.FromMinutes(10));

                    category.ItemCount = count;
                }
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
