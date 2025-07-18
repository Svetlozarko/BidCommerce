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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBid(int productId, decimal amount)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsBiddable);

            if (product == null)
                return NotFound("Product not found or not biddable");

            if (!product.BidEndTime.HasValue || product.BidEndTime < DateTime.UtcNow)
                return BadRequest("Auction has ended");

            decimal currentBid = product.CurrentBid ?? product.StartingPrice ?? 0;

            if (amount <= currentBid)
                return BadRequest("Bid must be higher than the current bid");

            // Create and save new Bid record
            var bid = new Bid
            {
                Amount = amount,
                ProductId = productId,
                BidderId = userId,
                PlacedAt = DateTime.UtcNow
            };

            _context.Bids.Add(bid);

            // Update product current bid
            product.CurrentBid = amount;

            await _context.SaveChangesAsync();

            // Notify clients in SignalR group
            await _hubContext.Clients.Group(productId.ToString())
                .SendAsync("ReceiveBid", User.Identity.Name, amount, bid.PlacedAt);

            return Ok(new { productId, amount });
        }

    }
}
