using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Infrastructure.Data;

namespace ShopInventory.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _db;
        public DashboardService(ApplicationDbContext db) => _db = db;

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6);

            var products = await _db.Product.ToListAsync();
            var variants = await _db.ProductVariant.ToListAsync();
            var sales = await _db.Sale.ToListAsync();

            var todaySales = sales.Where(s => s.SoldAt.Date == today).ToList();
            var last7Sales = sales.Where(s => s.SoldAt.Date >= sevenDaysAgo).ToList();

            // Last 7 days chart data
            var last7Days = Enumerable.Range(0, 7).Select(i =>
            {
                var date = today.AddDays(-6 + i);
                var daySales = last7Sales.Where(s => s.SoldAt.Date == date).ToList();
                return new DailySaleDto
                {
                    Day = date.ToString("MMM dd"),
                    Revenue = daySales.Sum(s => s.TotalRevenue),
                    Profit = daySales.Sum(s => s.TotalProfit),
                    UnitsSold = daySales.Sum(s => s.QuantitySold)
                };
            }).ToList();

            // Top 5 products by units sold
            var topProducts = sales
                .GroupBy(s => s.ProductName)
                .Select(g => new TopProductDto
                {
                    Name = g.Key,
                    UnitsSold = g.Sum(s => s.QuantitySold),
                    Revenue = g.Sum(s => s.TotalRevenue)
                })
                .OrderByDescending(t => t.UnitsSold)
                .Take(5)
                .ToList();

            // Low stock items (products + variants)
            var lowStockItems = new List<LowStockItemDto>();

            foreach (var p in products.Where(p => p.Quantity <= 3 && !p.HasVariants))
            {
                lowStockItems.Add(new LowStockItemDto
                {
                    Name = p.Name,
                    Quantity = p.Quantity,
                    Status = p.Quantity == 0 ? "Out of Stock" : "Low Stock"
                });
            }

            foreach (var v in variants.Where(v => v.Quantity <= 3))
            {
                var product = products.FirstOrDefault(p => p.Id == v.ProductId);
                lowStockItems.Add(new LowStockItemDto
                {
                    Name = product?.Name ?? "Unknown",
                    VariantName = v.Name,
                    Quantity = v.Quantity,
                    Status = v.Quantity == 0 ? "Out of Stock" : "Low Stock"
                });
            }

            return new DashboardDto
            {
                TotalProducts = products.Count,
                TotalVariants = variants.Count,
                LowStockCount = lowStockItems.Count(i => i.Status == "Low Stock"),
                OutOfStockCount = lowStockItems.Count(i => i.Status == "Out of Stock"),
                TodayRevenue = todaySales.Sum(s => s.TotalRevenue),
                TodayProfit = todaySales.Sum(s => s.TotalProfit),
                TotalRevenue = sales.Sum(s => s.TotalRevenue),
                TotalProfit = sales.Sum(s => s.TotalProfit),
                TodaySalesCount = todaySales.Sum(s => s.QuantitySold),
                Last7Days = last7Days,
                TopProducts = topProducts,
                LowStockItems = lowStockItems.OrderBy(i => i.Quantity).Take(8).ToList()
            };
        }
    }
}