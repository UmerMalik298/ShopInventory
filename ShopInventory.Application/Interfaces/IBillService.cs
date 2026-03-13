using ShopInventory.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.Interfaces
{
    public interface IBillService
    {
        Task<BillDto> CreateAsync(CreateBillDto dto);
        Task<List<BillDto>> GetAllAsync();
        Task<BillDto?> GetByIdAsync(Guid id);
        Task MarkPaidAsync(Guid billId);
        Task MarkUnpaidAsync(Guid billId);
        Task<string> GenerateReceiptHtmlAsync(Guid billId);
    }
}
