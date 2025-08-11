using BidCommerce.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BidCommerce.Controllers
{
    [Route("checkout")]
    public class CheckoutController : Controller
    {
        private readonly BidDb _dbContext;

        public CheckoutController(BidDb dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("{productId}")]
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _dbContext.Products
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            // Pass product info to your Razor view
            return View(product);
        }
    }
}

