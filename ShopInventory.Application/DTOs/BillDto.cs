using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.DTOs
{
    public class BillDto
    {
        public Guid Id { get; set; }
        public string BillNo { get; set; } = "";
        public DateTime BillDate { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }

        public string PaymentMethod { get; set; } = "Cash";
        public int PaymentStatus { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public string? Notes { get; set; }

        public List<BillItemDto> Items { get; set; } = new();
    }
}
