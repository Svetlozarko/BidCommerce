using BidCommerce.Models;

namespace BidCommerce.ViewModels
{
    public class ProductIndexViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int? SelectedCategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; } = "newest";
        public string? ListingType { get; set; }
        public string? SelectedCategoryName { get; set; }
        public List<int> UserWatchlistProductIds { get; set; }


    }
}