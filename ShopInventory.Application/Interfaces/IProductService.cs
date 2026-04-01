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
        Task<List<ProductDto>> FindDuplicatesAsync(string name, string sku);

        // ── Replaces the old GetPagedAsync(string, int, int) ──────────────
        // All filtering/sorting happens in SQLite — only PageSize rows returned
        Task<PagedResult<ProductDto>> GetPagedAsync(
            string search,
            int page,
            int pageSize,
            string? category = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? stockFilter = null,
            string? sortBy = null);

        // One cheap SELECT DISTINCT — no full table scan
        Task<List<string>> GetDistinctCategoriesAsync();
    }
}