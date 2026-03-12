using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.DTOs
{
    public class RecentSaleDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = "";
        public string? VariantName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public DateTime SoldAt { get; set; }
        public string? Notes { get; set; }
    }
}