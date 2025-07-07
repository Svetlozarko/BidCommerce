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
            new Category { CategoryId = 1, Name = "Electronics", Description = "Electronic devices and gadgets" },
            new Category { CategoryId = 2, Name = "Fashion", Description = "Clothing and accessories" },
            new Category { CategoryId = 3, Name = "Home", Description = "Furniture and home decor" },
            new Category { CategoryId = 4, Name = "Books", Description = "Books and literature" },
            new Category { CategoryId = 5, Name = "Toys", Description = "Toys and games for all ages" }
        );

        }
    }
}
