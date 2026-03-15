using ShopInventory.Domain.Entities.Common;

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

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;
        public decimal TotalProfit => (UnitPrice - CostPrice) * Quantity;
    }
}