namespace BidCommerce.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public string ImageUrl { get; set; }

        public bool IsPrimary { get; set; }
    }

}
