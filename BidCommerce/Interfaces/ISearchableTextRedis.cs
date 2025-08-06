namespace BidCommerce.Interfaces
{
    public interface ISearchableTextRedis
    {
        Task SaveProductAsync(int productId, string searchableText);
        Task<string?> GetProductAsync(int productId);
        Task DeleteProductAsync(int productId);

    }
}
