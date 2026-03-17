using ShopInventory.Application.DTOs;

namespace ShopInventory.Application.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllAsync();
        Task<List<ProductDto>> SearchAsync(string query);
        Task<ProductDto> AddAsync(CreateProductDto dto);
        Task UpdateQuantityAsync(Guid id, int delta);

        Task DeleteAsync(Guid id);
        Task UpdateAsync(ProductDto dto);


        Task<PagedResult<ProductDto>> GetPagedAsync(string search, int page, int pageSize);  

    }
}