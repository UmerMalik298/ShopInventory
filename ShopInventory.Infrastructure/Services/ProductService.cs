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

        // ── DELETE ────────────────────────────────────────────────────────
        public async Task DeleteAsync(Guid id)
        {
            var product = await _db.Product.FindAsync(id);
            if (product is null) return;
            _db.Product.Remove(product);
            await _db.SaveChangesAsync();
        }

        // ── GET ALL (kept for any callers that still need it) ─────────────
        public async Task<List<ProductDto>> GetAllAsync()
        {
            return await _db.Product
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => ToDto(p))
                .ToListAsync();
        }

        // ── SEARCH (kept for cart / billing quick-search) ─────────────────
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

        // ── ADD ───────────────────────────────────────────────────────────
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

        // ── UPDATE QTY ────────────────────────────────────────────────────
        public async Task UpdateQuantityAsync(Guid id, int delta)
        {
            var product = await _db.Product.FindAsync(id);
            if (product is null) return;
            product.Quantity = Math.Max(0, product.Quantity + delta);
            product.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // ── UPDATE ────────────────────────────────────────────────────────
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

        // ── FIND DUPLICATES ───────────────────────────────────────────────
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

        // ── DISTINCT CATEGORIES ───────────────────────────────────────────
        // SELECT DISTINCT Category ... — single tiny query, never a full scan
        public async Task<List<string>> GetDistinctCategoriesAsync()
        {
            return await _db.Product
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        // ── PAGED + FILTERED ──────────────────────────────────────────────
        // Everything runs as SQL inside SQLite.
        // Only PageSize rows ever come back — no matter how many are in the DB.
        public async Task<PagedResult<ProductDto>> GetPagedAsync(
            string search,
            int page,
            int pageSize,
            string? category = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? stockFilter = null,
            string? sortBy = null)
        {
            // Build query — nothing executes yet
            IQueryable<Product> q = _db.Product;

            // Search — uses IX_Products_Search index
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                q = q.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term) ||
                    (p.Category != null && p.Category.ToLower().Contains(term)));
            }

            // Category
            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(p => p.Category == category);

            // Price range
            if (minPrice.HasValue)
                q = q.Where(p => p.SalePrice >= minPrice.Value);
            if (maxPrice.HasValue)
                q = q.Where(p => p.SalePrice <= maxPrice.Value);

            // Stock filter
            q = stockFilter switch
            {
                "instock" => q.Where(p => p.Quantity > 10),
                "lowstock" => q.Where(p => p.Quantity > 0 && p.Quantity <= 10),
                "outofstock" => q.Where(p => p.Quantity <= 0),
                _ => q
            };

            // Sort
            q = sortBy switch
            {
                "price_asc" => q.OrderBy(p => p.SalePrice),
                "price_desc" => q.OrderByDescending(p => p.SalePrice),
                "qty_asc" => q.OrderBy(p => p.Quantity),
                "qty_desc" => q.OrderByDescending(p => p.Quantity),
                "name_desc" => q.OrderByDescending(p => p.Name),
                _ => q.OrderBy(p => p.Name)
            };

            // COUNT — one SQL COUNT(*) with all filters applied
            var totalCount = await q.CountAsync();

            // FETCH — only the rows for this page
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
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

        // ── HELPERS ───────────────────────────────────────────────────────
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

        private static string GenerateSku(string name) =>
            (name.Length >= 3 ? name[..3].ToUpper() : "SKU")
            + "-" + Guid.NewGuid().ToString()[..4].ToUpper();
    }
}