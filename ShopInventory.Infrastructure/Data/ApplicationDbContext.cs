using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.AppInformation;
using ShopInventory.Domain.Entities.Products;
using ShopInventory.Domain.Entities.SyncQueueItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ShopInventory.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppInfo> AppInfo { get; set; } = default!;
        public DbSet<Product> Product { get; set; } = default!;
        public DbSet<SyncQueueItem> SyncQueueItem { get; set; } = default!;
    }
}
