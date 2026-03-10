using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Sales;
using ShopInventory.Infrastructure.Data;

namespace ShopInventory.Infrastructure.Services
{
    public class SaleService : ISaleService
    {
        private readonly ApplicationDbContext _db;
        public SaleService(ApplicationDbContext db) => _db = db;

        public async Task<List<SaleDto>> GetAllAsync()
        {
            return await _db.Sale
                .OrderByDescending(s => s.SoldAt)
                .Select(s => MapToDto(s))
                .ToListAsync();
        }

        public async Task<List<SaleDto>> GetByProductIdAsync(Guid productId)
        {
            return await _db.Sale
                .Where(s => s.ProductId == productId)
                .OrderByDescending(s => s.SoldAt)
                .Select(s => MapToDto(s))
                .ToListAsync();
        }

        public async Task RecordSaleAsync(CreateSaleDto dto)
        {
            if (dto.ProductVariantId.HasValue)
            {
                var variant = await _db.ProductVariant.FindAsync(dto.ProductVariantId.Value);
                if (variant is null) throw new Exception("Variant not found");
                if (variant.Quantity < dto.QuantitySold) throw new Exception($"Only {variant.Quantity} in stock");
                variant.Quantity -= dto.QuantitySold;
                variant.IsSynced = false;
            }
            else
            {
                var product = await _db.Product.FindAsync(dto.ProductId);
                if (product is null) throw new Exception("Product not found");
                if (product.Quantity < dto.QuantitySold) throw new Exception($"Only {product.Quantity} in stock");
                product.Quantity -= dto.QuantitySold;
                product.IsSynced = false;
            }

            _db.Sale.Add(new Sale
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                ProductVariantId = dto.ProductVariantId,
                ProductName = dto.ProductName,
                VariantName = dto.VariantName,
                QuantitySold = dto.QuantitySold,
                SalePriceAtTime = dto.SalePriceAtTime,
                CostPriceAtTime = dto.CostPriceAtTime,
                SoldAt = DateTime.UtcNow,
                Notes = dto.Notes,
                IsSynced = false
            });

            await _db.SaveChangesAsync();
        }

        private static SaleDto MapToDto(Sale s) => new()
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductVariantId = s.ProductVariantId,
            ProductName = s.ProductName,
            VariantName = s.VariantName,
            QuantitySold = s.QuantitySold,
            SalePriceAtTime = s.SalePriceAtTime,
            CostPriceAtTime = s.CostPriceAtTime,
            TotalRevenue = s.TotalRevenue,
            TotalProfit = s.TotalProfit,
            SoldAt = s.SoldAt,
            Notes = s.Notes
        };
    }
}