using ShopInventory.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.Interfaces
{
    public interface ISaleService
    {
        Task<List<SaleDto>> GetAllAsync();
        Task<List<SaleDto>> GetByProductIdAsync(Guid productId);
        Task RecordSaleAsync(CreateSaleDto dto);
    }
}
