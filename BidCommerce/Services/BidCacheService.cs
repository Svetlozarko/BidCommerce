using BidCommerce.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace BidCommerce.Services
{
    public class BidCacheService
    {
        private readonly IDatabase _redisDb;

        public BidCacheService(IConnectionMultiplexer connection)
        {
            _redisDb = connection.GetDatabase();
        }

        private string GetBidsKey(int productId) => $"product:{productId}:bids";
        private string GetCurrentBidKey(int productId) => $"product:{productId}:currentBid";

        // Add a bid to Redis sorted set (score = amount)
        public async Task AddBidAsync(int productId, string bidderId, decimal amount, DateTime placedAt)
        {
            var bid = new
            {
                BidderId = bidderId,
                Amount = amount,
                PlacedAt = placedAt
            };

            // Serialize bid to JSON string to store as member
            string bidJson = JsonSerializer.Serialize(bid);

            // Use amount as the score for ordering bids in sorted set
            await _redisDb.SortedSetAddAsync(GetBidsKey(productId), bidJson, (double)amount);

            // Update current bid hash with latest info
            var hashEntries = new HashEntry[]
            {
                new HashEntry("BidderId", bidderId),
                new HashEntry("Amount", amount.ToString()),
                new HashEntry("PlacedAt", placedAt.ToString("o")) // ISO 8601 format
            };

            await _redisDb.HashSetAsync(GetCurrentBidKey(productId), hashEntries);
        }

        // Get latest/current bid info from Redis hash
        public async Task<(string BidderId, decimal Amount, DateTime PlacedAt)?> GetCurrentBidAsync(int productId)
        {
            var entries = await _redisDb.HashGetAllAsync(GetCurrentBidKey(productId));
            if (entries.Length == 0)
                return null;

            string bidderId = entries.FirstOrDefault(e => e.Name == "BidderId").Value;
            string amountStr = entries.FirstOrDefault(e => e.Name == "Amount").Value;
            string placedAtStr = entries.FirstOrDefault(e => e.Name == "PlacedAt").Value;

            if (decimal.TryParse(amountStr, out var amount) && DateTime.TryParse(placedAtStr, out var placedAt))
            {
                return (bidderId, amount, placedAt);
            }

            return null;
        }

        // Get last N bids for a product (descending by amount)
        public async Task<List<(string BidderId, decimal Amount, DateTime PlacedAt)>> GetRecentBidsAsync(int productId, int count = 10)
        {
            var results = await _redisDb.SortedSetRangeByRankAsync(GetBidsKey(productId), -count, -1, Order.Ascending);
            var bids = new List<(string, decimal, DateTime)>();

            foreach (var result in results)
            {
                try
                {
                    var bid = JsonSerializer.Deserialize<BidDto>(result)!;
                    bids.Add((bid.BidderId, bid.Amount, bid.PlacedAt));
                }
                catch
                {
                    // ignore malformed entries
                }
            }

            // SortedSetRangeByRankAsync with Order.Ascending returns lowest first, so reverse to get highest bids first
            bids.Reverse();

            return bids;
        }

        private class BidDto
        {
            public string BidderId { get; set; } = "";
            public decimal Amount { get; set; }
            public DateTime PlacedAt { get; set; }
        }
    }
}
