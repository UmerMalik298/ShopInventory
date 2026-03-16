using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure.Data;
using System.Net.Http.Json;
using Microsoft.Extensions.Http;

namespace ShopInventory.Infrastructure.Services
{
    public class SyncService : ISyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string Client = "ApiClient";

        public SyncService(ApplicationDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await client.GetAsync("https://www.google.com");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<SyncResult> GetLastSyncStatusAsync()
        {
            var lastProduct = await _db.Product.IgnoreQueryFilters()
                .Where(p => p.IsSynced)
                .MaxAsync(p => (DateTime?)p.LastSyncedAt);

            var lastBill = await _db.Bill.IgnoreQueryFilters()
                .Where(b => b.IsSynced)
                .MaxAsync(b => (DateTime?)b.LastSyncedAt);

            var lastSynced = new[] { lastProduct, lastBill }
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .DefaultIfEmpty()
                .Max();

            var unsynced =
                await _db.Product.IgnoreQueryFilters().CountAsync(p => !p.IsSynced) +
                await _db.ProductVariant.IgnoreQueryFilters().CountAsync(v => !v.IsSynced) +
                await _db.Sale.IgnoreQueryFilters().CountAsync(s => !s.IsSynced) +
                await _db.Bill.IgnoreQueryFilters().CountAsync(b => !b.IsSynced) +
                await _db.BillItem.IgnoreQueryFilters().CountAsync(i => !i.IsSynced);

            return new SyncResult
            {
                Success = unsynced == 0,
                LastSyncedAt = lastSynced == default ? null : lastSynced,
                TotalFailed = unsynced
            };
        }

        public async Task SyncAsync()
        {
            if (!await IsOnlineAsync()) return;

            // Order matters — parents before children
            await PushUnsyncedAsync<ShopInventory.Domain.Entities.Products.Product>(
                () => _db.Product.IgnoreQueryFilters().Where(p => !p.IsSynced).ToListAsync(),
                item => new ProductDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Sku = item.Sku,
                    CostPrice = item.CostPrice,
                    SalePrice = item.SalePrice,
                    Category = item.Category,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    ImagePath = item.ImagePath,
                    HasVariants = item.HasVariants
                },
                "api/sync/product"
            );

            await PushUnsyncedAsync<ShopInventory.Domain.Entities.Products.ProductVariant>(
                () => _db.ProductVariant.IgnoreQueryFilters().Where(v => !v.IsSynced).ToListAsync(),
                item => new ProductVariantDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    Name = item.Name,
                    Sku = item.Sku,
                    CostPrice = item.CostPrice,
                    SalePrice = item.SalePrice,
                    Quantity = item.Quantity,
                    Quality = item.Quality,
                    ImagePath = item.ImagePath
                },
                "api/sync/variant"
            );

            await PushUnsyncedAsync<ShopInventory.Domain.Entities.Sales.Sale>(
                () => _db.Sale.IgnoreQueryFilters().Where(s => !s.IsSynced).ToListAsync(),
                item => new SaleDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    VariantName = item.VariantName,
                    QuantitySold = item.QuantitySold,
                    SalePriceAtTime = item.SalePriceAtTime,
                    CostPriceAtTime = item.CostPriceAtTime,
                    TotalRevenue = item.TotalRevenue,
                    TotalProfit = item.TotalProfit,
                    SoldAt = item.SoldAt,
                    Notes = item.Notes
                },
                "api/sync/sale"
            );

            await PushUnsyncedBillsAsync();
        }

        // Generic push helper — constraint changed to BaseEntity
        private async Task PushUnsyncedAsync<TEntity>(
            Func<Task<List<TEntity>>> getUnsynced,
            Func<TEntity, object> toDto,
            string endpoint)
            where TEntity : ShopInventory.Domain.Entities.Common.BaseEntity
        {
            var unsynced = await getUnsynced();
            if (!unsynced.Any()) return;

            var client = _httpClientFactory.CreateClient(Client);

            foreach (var item in unsynced)
            {
                try
                {
                    var response = await client.PostAsJsonAsync(endpoint, toDto(item));
                    response.EnsureSuccessStatusCode();
                    item.IsSynced = true;
                    item.LastSyncedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Sync] {typeof(TEntity).Name} failed: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
        }

        // Bills need special handling — include Items
        private async Task PushUnsyncedBillsAsync()
        {
            var unsynced = await _db.Bill
                .IgnoreQueryFilters()
                .Include(b => b.Items)
                .Where(b => !b.IsSynced)
                .ToListAsync();

            if (!unsynced.Any()) return;

            var client = _httpClientFactory.CreateClient(Client);

            foreach (var bill in unsynced)
            {
                try
                {
                    var dto = new BillDto
                    {
                        Id = bill.Id,
                        BillNo = bill.BillNo,
                        BillDate = bill.BilledAt,                        // BilledAt (not BillDate)
                        CustomerName = bill.CustomerName,
                        CustomerPhone = bill.CustomerPhone,
                        PaymentMethod = bill.PaymentMethod.ToString(),   // enum → string
                        PaymentStatus = (int)bill.PaymentStatus,
                        SubTotal = bill.SubTotal,
                        DiscountAmount = bill.DiscountAmount,
                        GrandTotal = bill.TotalAmount,                   // TotalAmount (not GrandTotal)
                        Notes = bill.Notes,
                        Items = bill.Items.Select(i => new BillItemDto
                        {
                            Id = i.Id,
                           
                            ProductId = i.ProductId,
                            ProductVariantId = i.ProductVariantId,
                            ProductName = i.ProductName,
                            VariantName = i.VariantName,
                            UnitPrice = i.UnitPrice,
                            CostPriceAtTime = i.CostPrice,               // CostPrice (not CostPriceAtTime)
                            Quantity = i.Quantity
                        }).ToList()
                    };

                    var response = await client.PostAsJsonAsync("api/sync/bill", dto);
                    response.EnsureSuccessStatusCode();

                    bill.IsSynced = true;
                    bill.LastSyncedAt = DateTime.UtcNow;

                    foreach (var item in bill.Items)
                    {
                        item.IsSynced = true;
                        item.LastSyncedAt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Sync] Bill failed [{bill.BillNo}]: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}