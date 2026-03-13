using ShopInventory.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Billing
{
    public class BillItem : BaseEntity
    {
        public Guid BillId { get; set; }
        public Bill Bill { get; set; } = null!;

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
