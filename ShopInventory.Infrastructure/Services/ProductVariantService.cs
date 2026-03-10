using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Products;

using System;

namespace ShopInventory.Application.Services
{
    public class ProductVariantService : IProductVariantService
    {
        private readonly IDbContext _db;

        public ProductVariantService(IDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductVariantDto>> GetByProductIdAsync(Guid productId)
        {
            return await _db.ProductVariant
                .Where(v => v.ProductId == productId)
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    Name = v.Name,
                    Sku = v.Sku,
                    CostPrice = v.CostPrice,
                    SalePrice = v.SalePrice,
                    Quantity = v.Quantity,
                    Quality = v.Quality,
                    ImagePath = v.ImagePath
                }).ToListAsync();
        }

        public async Task AddAsync(CreateProductVariantDto dto)
        {
            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = dto.ProductId,
                Name = dto.Name,
                Sku = dto.Sku,
                CostPrice = dto.CostPrice,
                SalePrice = dto.SalePrice,
                Quantity = dto.Quantity,
                Quality = dto.Quality,
                ImagePath = dto.ImagePath
            };
            _db.ProductVariant.Add(variant);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateQuantityAsync(Guid id, int delta)
        {
            var v = await _db.ProductVariant.FindAsync(id);
            if (v is null) return;
            v.Quantity = Math.Max(0, v.Quantity + delta);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var v = await _db.ProductVariant.FindAsync(id);
            if (v is null) return;
            _db.ProductVariant.Remove(v);
            await _db.SaveChangesAsync();
        }
    }
}