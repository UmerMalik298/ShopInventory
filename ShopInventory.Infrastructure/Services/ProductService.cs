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

        public async Task DeleteAsync(Guid id)
        {
            var product = await _db.Product.FindAsync(id);
            if (product is null) return;

            _db.Product.Remove(product);
            await _db.SaveChangesAsync();
        }
        public async Task<List<ProductDto>> GetAllAsync()
        {
            return await _db.Product
                .Where(p => p.IsDeleted == false)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    CostPrice = p.CostPrice,
                    SalePrice = p.SalePrice,
                    Category = p.Category,
                    Unit = p.Unit,
                    Quantity = p.Quantity,
                    ImagePath = p.ImagePath,
                    HasVariants = p.HasVariants
                })
                .ToListAsync();
        }

        public async Task<List<ProductDto>> SearchAsync(string query)
        {
            var q = query.ToLower();
            return await _db.Product
                .Where(p => p.IsDeleted == false &&
                       (p.Name.ToLower().Contains(q) ||
                        p.Sku.ToLower().Contains(q) ||
                        (p.Category != null && p.Category.ToLower().Contains(q))))
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    CostPrice = p.CostPrice,
                    SalePrice = p.SalePrice,
                    Category = p.Category,
                    Unit = p.Unit,
                    Quantity = p.Quantity,
                    ImagePath = p.ImagePath,
                    HasVariants = p.HasVariants
                })
                .ToListAsync();
        }

        public async Task<ProductDto> AddAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Sku = dto.Sku,
                CostPrice = dto.CostPrice,
                SalePrice = dto.SalePrice,
                Category = dto.Category,
                Unit = dto.Unit,
                Quantity = dto.Quantity,
                ImagePath = dto.ImagePath,
                HasVariants = dto.HasVariants,
                IsDeleted = false,   
                IsSynced = false,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
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