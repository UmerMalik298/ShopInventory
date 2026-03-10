using ShopInventory.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Sales
{
    public class Sale : BaseEntity
    {
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string? VariantName { get; set; }
        public int QuantitySold { get; set; }
        public decimal SalePriceAtTime { get; set; }
        public decimal CostPriceAtTime { get; set; }
        public decimal TotalRevenue => SalePriceAtTime * QuantitySold;
        public decimal TotalProfit => (SalePriceAtTime - CostPriceAtTime) * QuantitySold;
        public DateTime SoldAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
