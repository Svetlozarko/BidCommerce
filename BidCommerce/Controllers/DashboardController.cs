using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace BidCommerce.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly BidDb _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(BidDb context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.OwnerId == currentUser.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var activeListings = userProducts
                .Where(p => p.BidEndTime == null || p.BidEndTime > DateTime.Now)
                .ToList();

            var pastListings = userProducts
                .Where(p => p.BidEndTime != null && p.BidEndTime <= DateTime.Now)
                .ToList();

            decimal totalRevenue = userProducts.Sum(p => p.BuyNowPrice ?? p.CurrentBid ?? p.StartingPrice ?? 0);

            var viewModel = new MyDashboardViewModel
            {
                CurrentUser = currentUser,
                ActiveListings = activeListings,
                PastListings = pastListings,
                TotalRevenue = totalRevenue,
                ActiveListingsCount = activeListings.Count,
                Country = currentUser.Country ?? "Unknown"
            };

            return View("~/Views/User/Dashboard.cshtml", viewModel);


        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index)); // or wherever your listings live
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string NickName, string Email, string PhoneNumber, string Location, string Description, string Website, IFormFile ProfilePhoto)
        {
            // Handle profile update logic here
            // Update user information in database
            // Handle file upload for profile photo

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNotifications(bool EmailNotifications, bool BidAlerts, bool MessageAlerts, bool MarketingEmails)
        {
            // Handle notification preferences update

            TempData["Success"] = "Notification preferences updated!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePrivacy(bool ShowEmail, bool ShowPhone, bool ShowLocation, bool PublicProfile)
        {
            // Handle privacy settings update

            TempData["Success"] = "Privacy settings updated!";
            return RedirectToAction("Index");
        }
    }
}
