using ShopInventory.Application.DTOs;

namespace ShopInventory.Application.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync();
        Task<List<ProductDto>> SearchAsync(string query);
        Task<ProductDto> AddAsync(CreateProductDto dto);
        Task UpdateQuantityAsync(Guid id, int delta);
    }
}