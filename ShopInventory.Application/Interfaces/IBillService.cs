using ShopInventory.Application.DTOs;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.Interfaces
{
    public interface IBillService
    {
        Task<Bill> CreateBillAsync(Bill bill);

        // These must match what BillList.razor and BillDetail.razor call:
        Task<List<Bill>> GetAllBillsAsync();
        Task<Bill?> GetBillByIdAsync(Guid id);
        Task DeleteBillAsync(Guid id);
        Task UpdatePaymentStatusAsync(Guid id, PaymentStatus status);
    }
}
