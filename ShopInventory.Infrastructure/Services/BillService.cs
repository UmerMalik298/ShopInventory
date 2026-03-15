// Infrastructure/Services/BillService.cs

using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;
using ShopInventory.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace ShopInventory.Infrastructure.Services
{
    public class BillService : IBillService
    {
        private readonly IDbContext _db;
        public BillService(IDbContext db) => _db = db;

        public async Task<Bill> CreateBillAsync(Bill bill)
        {
            // 1. Generate Bill Number
            var prefix = $"BILL-{DateTime.UtcNow:yyyyMMdd}-";
            var todayCount = await _db.Bill.CountAsync(b => b.BillNo.StartsWith(prefix));
            bill.BillNo = $"{prefix}{(todayCount + 1):D3}";

            // 2. Calculate totals
            bill.SubTotal = bill.Items.Sum(i => i.UnitPrice * i.Quantity);
            bill.TotalAmount = Math.Max(0, bill.SubTotal - bill.DiscountAmount);
            bill.BilledAt = DateTime.UtcNow;

            // 3. Save the bill
            _db.Bill.Add(bill);
            await _db.SaveChangesAsync();

            // 4. Deduct stock + create Sale records per item
            foreach (var item in bill.Items)
            {
                // Skip manual items (no real product in DB)
                if (item.ProductId == Guid.Empty)
                    continue;

                if (item.ProductVariantId.HasValue)
                {
                    var variant = await _db.ProductVariant.FindAsync(item.ProductVariantId.Value);
                    if (variant is not null)
                    {
                        variant.Quantity = Math.Max(0, variant.Quantity - item.Quantity);
                        variant.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var product = await _db.Product.FindAsync(item.ProductId);
                    if (product is not null)
                    {
                        product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Sale record for dashboard stats
                _db.Sale.Add(new Sale
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    VariantName = item.VariantName,
                    QuantitySold = item.Quantity,
                    SalePriceAtTime = item.UnitPrice,
                    CostPriceAtTime = item.CostPrice,
                    SoldAt = bill.BilledAt,
                    BillId = bill.Id,
                    BillNo = bill.BillNo,
                    Notes = bill.Notes
                });
            }

            await _db.SaveChangesAsync();
            return bill;
        }

        // ── These method names match IBillService and your existing components ──

        public async Task<List<Bill>> GetAllBillsAsync()
            => await _db.Bill
                .Include(b => b.Items)
                .OrderByDescending(b => b.BilledAt)
                .ToListAsync();

        public async Task<Bill?> GetBillByIdAsync(Guid id)
            => await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == id);

        public async Task DeleteBillAsync(Guid id)
        {
            var bill = await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (bill is null) return;
            _db.Bill.Remove(bill);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePaymentStatusAsync(Guid id, PaymentStatus status)
        {
            var bill = await _db.Bill.FindAsync(id);
            if (bill is null) return;
            bill.PaymentStatus = status;
            await _db.SaveChangesAsync();
        }


        // ── Draft bills ──────────────────────────────────────────────────

        public async Task<Bill> SaveDraftAsync(Bill bill)
        {
            bill.Status = BillStatus.Draft;
            bill.SubTotal = bill.Items.Sum(i => i.UnitPrice * i.Quantity);
            bill.TotalAmount = Math.Max(0, bill.SubTotal - bill.DiscountAmount);

            // If draft already exists (has an Id) → update it
            if (bill.Id != Guid.Empty)
            {
                var existing = await _db.Bill
                    .Include(b => b.Items)
                    .FirstOrDefaultAsync(b => b.Id == bill.Id);

                if (existing is not null)
                {
                    // Update header fields
                    existing.CustomerName = bill.CustomerName;
                    existing.CustomerPhone = bill.CustomerPhone;
                    existing.Notes = bill.Notes;
                    existing.DiscountAmount = bill.DiscountAmount;
                    existing.PaymentStatus = bill.PaymentStatus;
                    existing.PaymentMethod = bill.PaymentMethod;
                    existing.SubTotal = bill.SubTotal;
                    existing.TotalAmount = bill.TotalAmount;

                    // Replace items
                    _db.BillItem.RemoveRange(existing.Items);
                    existing.Items = bill.Items;

                    await _db.SaveChangesAsync();
                    return existing;
                }
            }

            // New draft
            bill.BillNo = $"DRAFT-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            bill.BilledAt = DateTime.UtcNow;
            _db.Bill.Add(bill);
            await _db.SaveChangesAsync();
            return bill;
        }

        public async Task<List<Bill>> GetAllDraftsAsync()
            => await _db.Bill
                .Include(b => b.Items)
                .Where(b => b.Status == BillStatus.Draft)
                .OrderByDescending(b => b.BilledAt)
                .ToListAsync();

        private async Task<string> GenerateBillNo()
        {
            var prefix = $"BILL-{DateTime.UtcNow:yyyyMMdd}-";
            var todayCount = await _db.Bill.CountAsync(b => b.BillNo.StartsWith(prefix));
            return $"{prefix}{(todayCount + 1):D3}";
        }

        private async Task DeductStockAndRecordSales(Bill bill)
        {
            foreach (var item in bill.Items)
            {
                if (item.ProductId == Guid.Empty) continue;

                if (item.ProductVariantId.HasValue)
                {
                    var variant = await _db.ProductVariant.FindAsync(item.ProductVariantId.Value);
                    if (variant is not null)
                    {
                        variant.Quantity = Math.Max(0, variant.Quantity - item.Quantity);
                        variant.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    var product = await _db.Product.FindAsync(item.ProductId);
                    if (product is not null)
                    {
                        product.Quantity = Math.Max(0, product.Quantity - item.Quantity);
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                _db.Sale.Add(new Sale
                {
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    VariantName = item.VariantName,
                    QuantitySold = item.Quantity,
                    SalePriceAtTime = item.UnitPrice,
                    CostPriceAtTime = item.CostPrice,
                    SoldAt = bill.BilledAt,
                    BillId = bill.Id,
                    BillNo = bill.BillNo,
                    Notes = bill.Notes
                });
            }
        }
        /// <summary>
        /// Converts a draft to a real saved bill:
        /// assigns a proper BillNo, deducts stock, creates Sale records.
        /// </summary>
        public async Task<Bill> PromoteDraftToSavedAsync(Guid id)
        {
            var bill = await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new Exception("Draft not found");

            bill.BillNo = await GenerateBillNo();
            bill.BilledAt = DateTime.UtcNow;
            bill.SubTotal = bill.Items.Sum(i => i.UnitPrice * i.Quantity);
            bill.TotalAmount = Math.Max(0, bill.SubTotal - bill.DiscountAmount);
            bill.Status = BillStatus.Finalised;

            await _db.SaveChangesAsync();

            await DeductStockAndRecordSales(bill);
            await _db.SaveChangesAsync();

            return bill;
        }
    }
}
