using ShopInventory.Domain.Entities.Common;
using ShopInventory.Domain.Entities.Enums;

namespace ShopInventory.Domain.Entities.Billing
{
    public class Bill : BaseEntity
    {
        public string BillNo { get; set; } = "";
        public DateTime BilledAt { get; set; } = DateTime.UtcNow;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Notes { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public BillStatus Status { get; set; } = BillStatus.Finalised;
        public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    }

  
}