using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace BidCommerce.Services
{
    public class BidHub : Hub
    {
        public async Task SendBid(int productId, string user, decimal amount)
        {
            await Clients.Group(productId.ToString()).SendAsync("ReceiveBid", user, amount, DateTime.UtcNow);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var productId = httpContext.Request.Query["productId"];

            if (!string.IsNullOrEmpty(productId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, productId);
            }

            await base.OnConnectedAsync();
        }
    }

}
