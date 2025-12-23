using Microsoft.EntityFrameworkCore;
using ShopInventory.Domain.Entities.AppInformation;
using ShopInventory.Domain.Entities.Products;
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
        DbSet<SyncQueueItem> SyncQueueItem { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
