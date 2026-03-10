using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure.Data;

namespace ShopInventory.Infrastructure.Services
{
    public class SyncService : ISyncService
    {
        private readonly ApplicationDbContext _db;

        public SyncService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var response = await client.GetAsync("https://www.google.com");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task SyncAsync()
        {
            if (!await IsOnlineAsync()) return;

            // Phase 1: push unsynced local changes to Supabase
            await PushUnsyncedProductsAsync();
            await PushUnsyncedVariantsAsync();

            // Phase 2: pull changes from Supabase into local
            // (build this when Supabase is ready)
        }

        private async Task PushUnsyncedProductsAsync()
        {
            var unsynced = await _db.Product
                .IgnoreQueryFilters()
                .Where(p => !p.IsSynced)
                .ToListAsync();

            foreach (var product in unsynced)
            {
                try
                {
                    // TODO: call Supabase API here when ready
                    // await _supabase.From<Product>().Upsert(product);

                    product.IsSynced = true;
                    product.LastSyncedAt = DateTime.UtcNow;
                }
                catch
                {
                    // leave IsSynced = false, retry next time
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task PushUnsyncedVariantsAsync()
        {
            var unsynced = await _db.ProductVariant
                .IgnoreQueryFilters()
                .Where(v => !v.IsSynced)
                .ToListAsync();

            foreach (var variant in unsynced)
            {
                try
                {
                    // TODO: call Supabase API here when ready
                    variant.IsSynced = true;
                    variant.LastSyncedAt = DateTime.UtcNow;
                }
                catch { }
            }

            await _db.SaveChangesAsync();
        }
    }
}