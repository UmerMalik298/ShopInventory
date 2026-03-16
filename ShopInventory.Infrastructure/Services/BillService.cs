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
            foreach (var req in productRequests)
            {
                products[req.ProductId].Quantity -= req.Quantity;
            }

            foreach (var req in variantRequests)
            {
                variants[req.VariantId].Quantity -= req.Quantity;
            }

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

        public string GenerateBillNo()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"BILL-{date}-{random}";
        }
    }
}
