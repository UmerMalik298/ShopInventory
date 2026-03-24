using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Products;
using ShopInventory.Infrastructure.Migrations;

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
                OldPrice = dto.OldPrice,
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

        private static ProductDto ToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            SalePrice = p.SalePrice,
            CostPrice = p.CostPrice,
            OldPrice = p.OldPrice,
            Quantity = p.Quantity,
            ImagePath = p.ImagePath,
            Category = p.Category,
            Unit = p.Unit
        };
        public async Task UpdateAsync(ProductDto dto)
        {
            var product = await _db.Product.FindAsync(dto.Id);
            if (product is null) return;

            product.Name = dto.Name;
            product.Sku = dto.Sku;
            product.Quantity = dto.Quantity;
            product.SalePrice = dto.SalePrice;
            product.OldPrice = dto.OldPrice;
            product.CostPrice = dto.CostPrice;
            product.Category = dto.Category;
            product.ImagePath = dto.ImagePath;
            product.Unit = dto.Unit;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }


        // Replace your existing GetPagedAsync method with this.
        // Everything else in ProductService.cs stays the same.

        public async Task<PagedResult<ProductDto>> GetPagedAsync(
            string? search, int page, int pageSize)
        {
            // IQueryable — nothing hits the DB until ToListAsync/CountAsync
            var query = _db.Product.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                // EF Core translates .ToLower().Contains() → SQL LOWER(col) LIKE '%term%'
                // The index on (Name, Sku, Category) makes this fast
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term) ||
                    (p.Category != null && p.Category.ToLower().Contains(term)));
            }

            // Both run as SQL — only the COUNT(*) and 20 rows ever come back
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)               // stable order required for correct paging
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto         // project in SQL — unused columns never loaded
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Quantity = p.Quantity,
                    SalePrice = p.SalePrice,
                    CostPrice = p.CostPrice,
                    Category = p.Category,
                    Unit = p.Unit,
                    ImagePath = p.ImagePath,
                    OldPrice = p.OldPrice
                })
                .ToListAsync();

            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<ProductDto>> FindDuplicatesAsync(string name, string sku)
        {
            var nameLower = name.Trim().ToLower();
            var skuLower = sku.Trim().ToLower();

            return await _db.Product
                .Where(p => p.IsActive &&
                    (p.Name.ToLower() == nameLower ||
                     (!string.IsNullOrEmpty(skuLower) && p.Sku.ToLower() == skuLower)))
                .Select(p => ToDto(p))
                .ToListAsync();
        }
        private static string GenerateSku(string name) =>
            (name.Length >= 3 ? name[..3].ToUpper() : "SKU")
            + "-" + Guid.NewGuid().ToString()[..4].ToUpper();
    }



}