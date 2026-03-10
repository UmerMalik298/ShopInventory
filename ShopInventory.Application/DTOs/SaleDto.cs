using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.DTOs
{
    public class SaleDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string? VariantName { get; set; }
        public int QuantitySold { get; set; }
        public decimal SalePriceAtTime { get; set; }
        public decimal CostPriceAtTime { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public DateTime SoldAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSaleDto
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string? VariantName { get; set; }
        public int QuantitySold { get; set; } = 1;
        public decimal SalePriceAtTime { get; set; }
        public decimal CostPriceAtTime { get; set; }
        public string? Notes { get; set; }
    }
}
