using Microsoft.AspNetCore.SignalR;

namespace BidCommerce.Services
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string recipientId, string message)
        {
            var senderId = Context.UserIdentifier;

            await Clients.User(recipientId).SendAsync("ReceiveMessage", senderId, message);
        }
    }

}
