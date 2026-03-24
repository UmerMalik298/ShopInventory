using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.AppInformation;
using ShopInventory.Domain.Entities.Billing;
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

        public DbSet<Bill> Bill => Set<Bill>();
        public DbSet<BillItem> BillItem => Set<BillItem>();

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

            modelBuilder.Entity<Bill>()
    .HasMany(b => b.Items)
    .WithOne(i => i.Bill)
    .HasForeignKey(i => i.BillId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.BillNo)
                .IsUnique();

            modelBuilder.Entity<BillItem>()
    .Ignore(i => i.TotalPrice)
    .Ignore(i => i.TotalProfit);


            modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.Name, p.Sku, p.Category })
            .HasDatabaseName("IX_Products_Search");
        }

    }
}