using ShopInventory.Domain.Entities.Common;
using ShopInventory.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Billing
{
    public class Bill : BaseEntity
    {
        public string BillNo { get; set; } = "";

        public DateTime BillDate { get; set; } = DateTime.UtcNow;

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
        public BillStatus BillStatus { get; set; } = BillStatus.Finalised;

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? Notes { get; set; }

        public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
    }
}
