using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BidCommerce.Models;
using System.Reflection.Emit;

namespace BidCommerce.Data
{
    public class BidDb : IdentityDbContext<ApplicationUser>
    {
        public BidDb(DbContextOptions<BidDb> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }    
        public DbSet<Category> Categories { get; set; }
        public DbSet<WatchlistItem> WatchlistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable(name: "Users");
            });
            builder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "Roles");
            });
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable(name: "UserRoles");
            });
            builder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable(name: "UserClaims");
            });
            builder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable(name: "UserLogins");
            });
            builder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable(name: "RoleClaims");
            });
            builder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable(name: "UserTokens");
            });

            builder.Entity<Category>().HasData(
    // 1 - 4
    new Category { CategoryId = 1, Name = "Electronics", Description = "Electronic devices and gadgets" },
    new Category { CategoryId = 2, Name = "Fashion", Description = "Clothing and accessories" },
    new Category { CategoryId = 3, Name = "Home", Description = "Furniture and home decor" },
    new Category { CategoryId = 4, Name = "Books", Description = "Books and literature" },

    // 5 - 8
    new Category { CategoryId = 5, Name = "Toys", Description = "Toys and games for all ages" },
    new Category { CategoryId = 6, Name = "Sports", Description = "Sporting goods and fitness equipment" },
    new Category { CategoryId = 7, Name = "Beauty", Description = "Beauty products and skincare" },
    new Category { CategoryId = 8, Name = "Automotive", Description = "Car parts and accessories" },

    // 9 - 12
    new Category { CategoryId = 9, Name = "Garden", Description = "Gardening tools and outdoor decor" },
    new Category { CategoryId = 10, Name = "Groceries", Description = "Food and beverages" },
    new Category { CategoryId = 11, Name = "Pet Supplies", Description = "Products for pets and animals" },
    new Category { CategoryId = 12, Name = "Health", Description = "Health and wellness products" },

    // 13 - 16
    new Category { CategoryId = 13, Name = "Music", Description = "Musical instruments and accessories" },
    new Category { CategoryId = 14, Name = "Office Supplies", Description = "Products for school and office" },
    new Category { CategoryId = 15, Name = "Jewelry", Description = "Rings, necklaces, and other jewelry" },
    new Category { CategoryId = 16, Name = "Art", Description = "Art supplies and creative materials" },

    // 17 - 20
    new Category { CategoryId = 17, Name = "Travel", Description = "Travel accessories and luggage" },
    new Category { CategoryId = 18, Name = "Baby Products", Description = "Products for babies and infants" },
    new Category { CategoryId = 19, Name = "Video Games", Description = "Games and gaming consoles" },
    new Category { CategoryId = 20, Name = "Tools", Description = "Hand tools and power tools" }
);


        }
    }
}
