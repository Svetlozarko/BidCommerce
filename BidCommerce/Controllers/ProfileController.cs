using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims; // Added for DateTime

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

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var seller = await _context.Users
                .Include(u => u.Products) // For listings and sales
                    .ThenInclude(p => p.Category)
                .Include(u => u.Followers) // To get follower count
                .Include(u => u.Following) // Optional, if you want to show following count
                .FirstOrDefaultAsync(u => u.Id == id);

            if (seller == null)
                return NotFound();

            var currentListings = seller.Products?
                .Where(p => p.BidEndTime == null || p.BidEndTime > DateTime.Now)
                .ToList() ?? new List<Product>();

            int totalSalesCount = seller.Products?.Count() ?? 0;

            // Determine if current logged-in user follows this seller
            bool isFollowing = false;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                isFollowing = await _context.Followers
                    .AnyAsync(f => f.FollowerId == currentUserId && f.FollowedId == id);
            }

            var viewModel = new SellerProfileViewModel
            {
                Seller = seller,
                CurrentListings = currentListings,
                TotalSalesCount = totalSalesCount,
                IsFollowing = isFollowing,
                // Optionally add these if you want to show follower counts in the ViewModel separately:
                FollowersCount = seller.Followers?.Count ?? 0,
                FollowingCount = seller.Following?.Count ?? 0
            };

            return View("~/Views/Profile/Seller.cshtml", viewModel);
        }

    }
}
