using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Products;

namespace ShopInventory.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly IDbContext _db;

        public ProductService(IDbContext db) => _db = db;

        public async Task<List<ProductDto>> GetAllAsync()
        {
            return await _db.Product
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => ToDto(p))
                .ToListAsync();
        }

        public async Task<List<ProductDto>> SearchAsync(string query)
        {
            var q = query.Trim().ToLower();
            return await _db.Product
                .Where(p => p.IsActive && (
                    p.Name.ToLower().Contains(q) ||
                    p.Sku.ToLower().Contains(q) ||
                    (p.Category != null && p.Category.ToLower().Contains(q))))
                .OrderBy(p => p.Name)
                .Select(p => ToDto(p))
                .ToListAsync();
        }

        public async Task<ProductDto> AddAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Sku = string.IsNullOrWhiteSpace(dto.Sku) ? GenerateSku(dto.Name) : dto.Sku,
                SalePrice = dto.SalePrice,
                CostPrice = dto.CostPrice,
                Quantity = dto.Quantity,
                ImagePath = dto.ImagePath,
                Category = dto.Category,
                Unit = dto.Unit
            };

            _db.Product.Add(product);
            await _db.SaveChangesAsync();
            return ToDto(product);
        }

        public async Task UpdateQuantityAsync(Guid id, int delta)
        {
            var product = await _db.Product.FindAsync(id);
            if (product is null) return;

            product.Quantity = Math.Max(0, product.Quantity + delta);
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ── helpers ────────────────────────────────────────────────────
        private static ProductDto ToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            SalePrice = p.SalePrice,
            CostPrice = p.CostPrice,
            Quantity = p.Quantity,
            ImagePath = p.ImagePath,
            Category = p.Category,
            Unit = p.Unit
        };

        private static string GenerateSku(string name) =>
            (name.Length >= 3 ? name[..3].ToUpper() : "SKU")
            + "-" + Guid.NewGuid().ToString()[..4].ToUpper();
    }
}