using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.AppInformation;
using ShopInventory.Domain.Entities.Products;
using ShopInventory.Domain.Entities.Sales;
using ShopInventory.Domain.Entities.SyncQueueItems;

namespace ShopInventory.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<AppInfo> AppInfo => Set<AppInfo>();
        public DbSet<Product> Product => Set<Product>();
        public DbSet<ProductVariant> ProductVariant => Set<ProductVariant>();
        public DbSet<Sale> Sale => Set<Sale>();
        public DbSet<SyncQueueItem> SyncQueueItem => Set<SyncQueueItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .HasQueryFilter(p => !p.IsDeleted);

            modelBuilder.Entity<ProductVariant>()
                .HasQueryFilter(v => !v.IsDeleted);

            modelBuilder.Entity<Sale>()
                .HasQueryFilter(s => !s.IsDeleted);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // TotalRevenue and TotalProfit are computed, not stored
            modelBuilder.Entity<Sale>()
                .Ignore(s => s.TotalRevenue)
                .Ignore(s => s.TotalProfit);
        }
    }
}