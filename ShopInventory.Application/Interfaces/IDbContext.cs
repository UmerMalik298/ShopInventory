using Microsoft.EntityFrameworkCore;
using ShopInventory.Domain.Entities.AppInformation;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Products;
using ShopInventory.Domain.Entities.Sales;
using ShopInventory.Domain.Entities.SyncQueueItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.Interfaces
{
    public interface IDbContext
    {
        DbSet<AppInfo> AppInfo { get; }
        DbSet<Product> Product { get; }

        DbSet<ProductVariant> ProductVariant { get; }
        DbSet<Sale> Sale { get; }
        DbSet<SyncQueueItem> SyncQueueItem { get; }
        
        DbSet<Bill> Bill { get; }
        DbSet<BillItem> BillItem { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
