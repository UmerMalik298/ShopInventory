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
            bill.Id = Guid.NewGuid();
            bill.BillNo = GenerateBillNo();
            bill.BilledAt = DateTime.UtcNow;

            bill.SubTotal = bill.Items.Sum(i => i.UnitPrice * i.Quantity);
            bill.TotalAmount = bill.SubTotal - bill.DiscountAmount;

            foreach (var item in bill.Items)
            {
                item.Id = Guid.NewGuid();
                item.BillId = bill.Id;
            }

            _db.Bill.Add(bill);
            await _db.SaveChangesAsync();
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
