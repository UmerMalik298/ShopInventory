using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShopInventory.Domain.Entities.Enums;

namespace ShopInventory.Application.DTOs
{
    public class BillListDto
    {
        public Guid Id { get; set; }
        public string BillNo { get; set; } = "";
        public string? CustomerName { get; set; }
        public DateTime BilledAt { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public bool IsDraft { get; set; }
        public int ItemCount { get; set; }
    }
}
