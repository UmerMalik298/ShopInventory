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
        Task<List<Bill>> GetAllBillsAsync();
        Task<Bill?> GetBillByIdAsync(Guid id);
        Task<Bill> CreateBillAsync(Bill bill);
        Task UpdatePaymentStatusAsync(Guid billId, PaymentStatus status);
        Task DeleteBillAsync(Guid id);
        string GenerateBillNo();
    }
}
