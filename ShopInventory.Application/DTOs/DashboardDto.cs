using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.DTOs
{
    public class DashboardDto
    {
        public int TotalProducts { get; set; }
        public int TotalVariants { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal TodayProfit { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TodaySalesCount { get; set; }
        public List<DailySaleDto> Last7Days { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<LowStockItemDto> LowStockItems { get; set; } = new();

    }

    public class DailySaleDto
    {
        public string Day { get; set; } = "";
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int UnitsSold { get; set; }
    }

    public class TopProductDto
    {
        public string Name { get; set; } = "";
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LowStockItemDto
    {
        public string Name { get; set; } = "";
        public string? VariantName { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = "";
    }
}