using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System; // Added for DateTime

namespace BidCommerce.Controllers
{
    public class ProfileController : Controller
    {
        private readonly BidDb _context;

        public ProfileController(BidDb context)
        {
            _context = context;
        }

        public async Task<IActionResult> Seller(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var seller = await _context.Users
                .Include(u => u.Products) // Include products to calculate sales/listings
                    .ThenInclude(p => p.Category) // Include category for product display
                .FirstOrDefaultAsync(u => u.Id == id);

            if (seller == null)
                return NotFound();

            // Filter for current/active listings
            var currentListings = seller.Products?
                .Where(p => p.BidEndTime == null || p.BidEndTime > DateTime.Now)
                .ToList() ?? new List<Product>();

            // Calculate total sales count and value from *all* products listed by the seller
            // In a real app, you'd filter for 'sold' products, which requires a 'status' field
            var allSellerProducts = seller.Products ?? new List<Product>();
            int totalSalesCount = allSellerProducts.Count(); // Counting all listed products for simplicity
            decimal totalSalesValue = allSellerProducts
                .Sum(p => p.BuyNowPrice ?? p.CurrentBid ?? p.StartingPrice ?? 0); // Summing up prices

            var viewModel = new SellerProfileViewModel
            {
                Seller = seller,
                CurrentListings = currentListings,
                TotalSalesCount = totalSalesCount,
            };

            return View("~/Views/Profile/Seller.cshtml", viewModel);
        }
    }
}
