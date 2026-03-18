using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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


        Task<Bill> SaveDraftAsync(Bill bill);


  
        Task UpdateDraftAsync(Guid draftId, Bill updatedBill);

    }
}
