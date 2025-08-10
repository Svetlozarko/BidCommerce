using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BidCommerce.Controllers
{
    [Authorize]
    public class BidsController : Controller
    {
        private readonly BidDb _context;
        private readonly IHubContext<BidHub> _hubContext;

        public BidsController(BidDb context, IHubContext<BidHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBid(int productId, decimal amount, [FromServices] BidCacheService bidCacheService)
        {
            var userId = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound("Product not found");

            if (!product.IsBiddable)
                return BadRequest("Product is not biddable");


            if (!product.BidEndTime.HasValue || product.BidEndTime < DateTime.UtcNow)
                return BadRequest("Auction has ended");

            var currentBid = await bidCacheService.GetCurrentBidAsync(productId);
            decimal highestBid = currentBid?.Amount ?? product.StartingPrice ?? 0;

            if (amount <= highestBid)
                return BadRequest("Bid must be higher than the current bid");

            // Add bid to Redis cache
            await bidCacheService.AddBidAsync(productId, userId, amount, DateTime.UtcNow);

            // Optionally update product's CurrentBid in DB (can defer with a background service)
            product.CurrentBid = amount;    
            await _context.SaveChangesAsync();

            // Notify clients
            await _hubContext.Clients.Group(productId.ToString())
                .SendAsync("ReceiveBid", userId, amount, DateTime.UtcNow);
            Console.WriteLine($"Bid placed by {userId} for product {productId}: {amount}");
            return Ok(new { productId, amount });
        }
        


    }
}
