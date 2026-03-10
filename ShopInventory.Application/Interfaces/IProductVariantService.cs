using ShopInventory.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Application.Interfaces
{
    public interface IProductVariantService
    {
        Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId);
        Task AddAsync(CreateProductVariantDto dto);
        Task UpdateQuantityAsync(Guid id, int delta);
        Task DeleteAsync(Guid id);
    }
}