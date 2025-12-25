using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Enums;

namespace ShopInventory.Infrastructure.Services
{
    public class SyncService
    {
        private readonly IDbContext _db;
        private readonly HttpClient _http;

        public SyncService(IDbContext db, HttpClient http)
        {
            _db = db;
            _http = http;
        }

        public async Task SyncPendingProducts()
        {
            var pendingItems = await _db.SyncQueueItem
                .Where(x => x.SyncStatusId == (int)SyncStatus.Pending)
                .ToListAsync();

            foreach (var item in pendingItems)
            {
                var response = await _http.PostAsync(
                    "api/sync/product",
                    new StringContent(item.PayloadJson, Encoding.UTF8, "application/json")
                );

                if (response.IsSuccessStatusCode)
                {
                    item.SyncStatusId = (int)SyncStatus.Synced;
                }
                else
                {
                    item.AttemptCount++;
                }
            }

            await _db.SaveChangesAsync();
        }
    }

}
