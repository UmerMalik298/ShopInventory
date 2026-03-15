using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.DTOs
{
    public class BillItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid? ProductVariantId { get; set; }

        public string ProductName { get; set; } = "";
        public string? VariantName { get; set; }
        public string? Sku { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal CostPriceAtTime { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
