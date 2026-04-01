using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;

using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;
using ShopInventory.Domain.Entities.Products;
using ShopInventory.Domain.Entities.Sales;
using ShopInventory.Infrastructure.Data;
using System;
using System.Net;
using System.Text;

namespace ShopInventory.Infrastructure.Services
{
    public class BillService : IBillService
    {
        private readonly ApplicationDbContext _db;

        public BillService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Bill>> GetAllBillsAsync()
        {
            return await _db.Bill
                .Include(b => b.Items)
                .OrderByDescending(b => b.BilledAt)
                .ToListAsync();
        }

        public async Task<Bill?> GetBillByIdAsync(Guid id)
        {
            return await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Bill> CreateBillAsync(Bill bill)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // Normal products only
            var productRequests = bill.Items
                .Where(i => i.ProductId != Guid.Empty && !i.ProductVariantId.HasValue)
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Name = g.First().ProductName
                })
                .ToList();

            // Variant products only
            var variantRequests = bill.Items
                .Where(i => i.ProductId != Guid.Empty && i.ProductVariantId.HasValue)
                .GroupBy(i => i.ProductVariantId!.Value)
                .Select(g => new
                {
                    VariantId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    ProductName = g.First().ProductName,
                    VariantName = g.First().VariantName
                })
                .ToList();

            var productIds = productRequests.Select(x => x.ProductId).ToList();
            var variantIds = variantRequests.Select(x => x.VariantId).ToList();

            var products = await _db.Product
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var variants = await _db.ProductVariant
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            // Validate normal product stock
            foreach (var req in productRequests)
            {
                if (!products.TryGetValue(req.ProductId, out var product))
                    throw new InvalidOperationException($"{req.Name} no longer exists.");

                if (product.Quantity <= 0)
                    throw new InvalidOperationException($"{product.Name} is out of stock.");

                if (req.Quantity > product.Quantity)
                    throw new InvalidOperationException(
                        $"Only {product.Quantity} item(s) available for {product.Name}.");
            }

            // Validate variant stock
            foreach (var req in variantRequests)
            {
                if (!variants.TryGetValue(req.VariantId, out var variant))
                    throw new InvalidOperationException(
                        $"{req.ProductName} ({req.VariantName}) no longer exists.");

                if (variant.Quantity <= 0)
                    throw new InvalidOperationException(
                        $"{variant.Product.Name} ({variant.Name}) is out of stock.");

                if (req.Quantity > variant.Quantity)
                    throw new InvalidOperationException(
                        $"Only {variant.Quantity} item(s) available for {variant.Product.Name} ({variant.Name}).");
            }

            // Reduce stock
            //foreach (var req in productRequests)
            //{
            //    products[req.ProductId].Quantity -= req.Quantity;
            //}

            //foreach (var req in variantRequests)
            //{
            //    variants[req.VariantId].Quantity -= req.Quantity;
            //}

            bill.Id = Guid.NewGuid();
            bill.BillNo = GenerateBillNo();
            bill.BilledAt = DateTime.UtcNow;
            bill.SubTotal = bill.Items.Sum(i => i.UnitPrice * i.Quantity);
            bill.TotalAmount = Math.Max(0, bill.SubTotal - bill.DiscountAmount);

            foreach (var item in bill.Items)
            {
                item.Id = Guid.NewGuid();
                item.BillId = bill.Id;
            }

            _db.Bill.Add(bill);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return bill;
        }
        public async Task DeductInventoryAsync(Guid billId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var bill = await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
                throw new InvalidOperationException("Bill not found.");

            var productRequests = bill.Items
                .Where(i => i.ProductId != Guid.Empty && !i.ProductVariantId.HasValue)
                .GroupBy(i => i.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Name = g.First().ProductName
                }).ToList();

            var variantRequests = bill.Items
                .Where(i => i.ProductId != Guid.Empty && i.ProductVariantId.HasValue)
                .GroupBy(i => i.ProductVariantId!.Value)
                .Select(g => new {
                    VariantId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    ProductName = g.First().ProductName,
                    VariantName = g.First().VariantName
                }).ToList();

            var productIds = productRequests.Select(x => x.ProductId).ToList();
            var variantIds = variantRequests.Select(x => x.VariantId).ToList();

            var products = await _db.Product
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var variants = await _db.ProductVariant
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            // Validate first
            foreach (var req in productRequests)
            {
                if (!products.TryGetValue(req.ProductId, out var product))
                    throw new InvalidOperationException($"{req.Name} no longer exists.");
                if (req.Quantity > product.Quantity)
                    throw new InvalidOperationException(
                        $"Only {product.Quantity} item(s) available for {product.Name}.");
            }

            foreach (var req in variantRequests)
            {
                if (!variants.TryGetValue(req.VariantId, out var variant))
                    throw new InvalidOperationException(
                        $"{req.ProductName} ({req.VariantName}) no longer exists.");
                if (req.Quantity > variant.Quantity)
                    throw new InvalidOperationException(
                        $"Only {variant.Quantity} available for {variant.Product.Name} ({variant.Name}).");
            }

            // Deduct
            foreach (var req in productRequests)
                products[req.ProductId].Quantity -= req.Quantity;

            foreach (var req in variantRequests)
                variants[req.VariantId].Quantity -= req.Quantity;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task<Bill> UpdateBillAsync(Guid billId, Bill updatedBill)
        {
            var existing = await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (existing == null)
                throw new InvalidOperationException("Bill not found.");

            existing.CustomerName = updatedBill.CustomerName;
            existing.CustomerPhone = updatedBill.CustomerPhone;
            existing.Notes = updatedBill.Notes;
            existing.DiscountAmount = updatedBill.DiscountAmount;
            existing.PaymentStatus = updatedBill.PaymentStatus;
            existing.PaymentMethod = updatedBill.PaymentMethod;
            existing.SubTotal = updatedBill.Items.Sum(i => i.UnitPrice * i.Quantity);
            existing.TotalAmount = Math.Max(0, existing.SubTotal - existing.DiscountAmount);
            existing.UpdatedAt = DateTime.UtcNow;

            // Replace all items
            _db.BillItem.RemoveRange(existing.Items);

            foreach (var item in updatedBill.Items)
            {
                existing.Items.Add(new BillItem
                {
                    Id = Guid.NewGuid(),
                    BillId = billId,
                    ProductId = item.ProductId,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = item.ProductName,
                    VariantName = item.VariantName,
                    UnitPrice = item.UnitPrice,
                    CostPrice = item.CostPrice,
                    Quantity = item.Quantity
                });
            }

            await _db.SaveChangesAsync();
            return existing;
        }
        public async Task UpdatePaymentStatusAsync(Guid billId, PaymentStatus status)
        {
            var bill = await _db.Bill.FindAsync(billId);
            if (bill != null)
            {
                bill.PaymentStatus = status;
                await _db.SaveChangesAsync();
            }
        }

        public async Task DeleteBillAsync(Guid id)
        {
            var bill = await _db.Bill.FindAsync(id);
            if (bill != null)
            {
                _db.Bill.Remove(bill);
                await _db.SaveChangesAsync();
            }
        }
        public async Task<Bill> SaveDraftAsync(Bill bill)
        {
            // Generate all IDs fresh — don't reuse anything from the form
            bill.Id = Guid.NewGuid();
            bill.BillNo = $"DRAFT-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            bill.BilledAt = DateTime.UtcNow;
            bill.IsDraft = true;
            bill.SubTotal = bill.Items.Sum(i => i.UnitPrice * i.Quantity);
            bill.TotalAmount = Math.Max(0, bill.SubTotal - bill.DiscountAmount);

            // Detach items from any existing tracking and assign fresh IDs
            foreach (var item in bill.Items)
            {
                item.Id = Guid.NewGuid();
                item.BillId = bill.Id;

                // Detach if EF is already tracking this entity
                var entry = _db.Entry(item);
                if (entry.State != EntityState.Detached)
                    entry.State = EntityState.Detached;
            }

            // Add as completely new — no Update, no Attach
            _db.Bill.Add(bill);
            await _db.SaveChangesAsync();
            return bill;
        }

        public async Task<PagedResult<BillListDto>> GetPagedBillsAsync(
                   string? search, string filter, int page, int pageSize)
        {
            // FIX: removed .Include(b => b.Items) — that was loading ALL bill
            // items for every bill on the page. For 10 bills × 5 items = 50
            // unnecessary rows fetched and immediately thrown away.
            var query = _db.Bill.AsNoTracking().AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b =>
                    b.BillNo.ToLower().Contains(term) ||
                    (b.CustomerName != null && b.CustomerName.ToLower().Contains(term)));
            }

            // Status filter
            query = filter switch
            {
                "unpaid" => query.Where(b => !b.IsDraft && b.PaymentStatus == PaymentStatus.Unpaid),
                "paid" => query.Where(b => !b.IsDraft && b.PaymentStatus == PaymentStatus.Paid),
                "partial" => query.Where(b => !b.IsDraft && b.PaymentStatus == PaymentStatus.PartiallyPaid),
                "draft" => query.Where(b => b.IsDraft),
                _ => query
            };

            // COUNT — one SQL COUNT(*) with all filters applied
            var totalCount = await query.CountAsync();

            // FETCH — project only the 8 columns the list UI needs.
            // b.Items.Count() translates to a SQL COUNT subquery — no items loaded.
            var items = await query
                .OrderByDescending(b => b.BilledAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BillListDto
                {
                    Id = b.Id,
                    BillNo = b.BillNo,
                    CustomerName = b.CustomerName,
                    BilledAt = b.BilledAt,
                    TotalAmount = b.TotalAmount,
                    PaymentStatus = b.PaymentStatus,
                    IsDraft = b.IsDraft,
                    ItemCount = b.Items.Count()
                })
                .ToListAsync();

            return new PagedResult<BillListDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        public async Task UpdateDraftAsync(Guid draftId, Bill updatedBill)
        {
            var existing = await _db.Bill
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == draftId);

            if (existing == null) return;

            // Update fields
            existing.CustomerName = updatedBill.CustomerName;
            existing.CustomerPhone = updatedBill.CustomerPhone;
            existing.Notes = updatedBill.Notes;
            existing.DiscountAmount = updatedBill.DiscountAmount;
            existing.PaymentStatus = updatedBill.PaymentStatus;
            existing.PaymentMethod = updatedBill.PaymentMethod;
            existing.SubTotal = updatedBill.Items.Sum(i => i.UnitPrice * i.Quantity);
            existing.TotalAmount = Math.Max(0, existing.SubTotal - existing.DiscountAmount);
            existing.UpdatedAt = DateTime.UtcNow;

            // Replace items — remove old, add new
            _db.BillItem.RemoveRange(existing.Items);

            foreach (var item in updatedBill.Items)
            {
                item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
                item.BillId = draftId;
                existing.Items.Add(item);
            }

            await _db.SaveChangesAsync();
        }
        public string GenerateBillNo()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"BILL-{date}-{random}";
        }
    }
}
