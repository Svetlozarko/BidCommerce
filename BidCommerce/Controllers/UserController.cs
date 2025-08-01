using BidCommerce.Data;
using BidCommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RateLimiterLib;
using RateLimiterLib.Enums;

namespace BidCommerce.Controllers
{
    public class UserController : Controller
    {
        public readonly BidDb _context;
        public UserController(BidDb context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RateLimit]
        public async Task<IActionResult> ToggleFollow([FromBody] string sellerId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            if (currentUserId == sellerId)
                return BadRequest("You cannot follow yourself.");

            // Load both users
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            var sellerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == sellerId);

            if (currentUser == null || sellerUser == null)
                return NotFound("User not found.");

            var existingFollow = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowedId == sellerId);

            bool isFollowing;

            if (existingFollow != null)
            {
                // Unfollow
                _context.Followers.Remove(existingFollow);
                await _context.SaveChangesAsync();
                isFollowing = false;
            }
            else
            {
                // Follow
                var follow = new Follower
                {
                    FollowerId = currentUserId,
                    FollowedId = sellerId
                };

                _context.Followers.Add(follow);
                await _context.SaveChangesAsync();
                isFollowing = true;
            }

            // Recalculate followers count after the operation
            var followersCount = await _context.Followers.CountAsync(f => f.FollowedId == sellerId);

            // Optionally update sellerUser.FollowersCount if you track it in DB
            sellerUser.FollowersCount = followersCount;
            _context.Users.Update(sellerUser);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                following = isFollowing,
                followersCount = followersCount
            });
        }

    }
}
