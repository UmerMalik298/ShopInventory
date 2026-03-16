using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopInventory.Application.DTOs;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Billing;
using ShopInventory.Domain.Entities.Enums;
using ShopInventory.Domain.Entities.Products;
using ShopInventory.Domain.Entities.Sales;

namespace ShopInventory.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly IDbContext _db;

        public SyncController(IDbContext db)
        {
            _db = db;
        }

        [HttpPost("product")]
        public async Task<IActionResult> SyncProduct([FromBody] ProductDto dto)
        {
            var existing = await _db.Product.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (existing == null)
            {
                _db.Product.Add(new Product
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Sku = dto.Sku,
                    CostPrice = dto.CostPrice,
                    SalePrice = dto.SalePrice,
                    Category = dto.Category,
                    Unit = dto.Unit,
                    Quantity = dto.Quantity,
                    ImagePath = dto.ImagePath,
                    HasVariants = dto.HasVariants,
                    IsActive = true,
                    IsSynced = true,
                    LastSyncedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Name = dto.Name;
                existing.Sku = dto.Sku;
                existing.CostPrice = dto.CostPrice;
                existing.SalePrice = dto.SalePrice;
                existing.Category = dto.Category;
                existing.Unit = dto.Unit;
                existing.Quantity = dto.Quantity;
                existing.ImagePath = dto.ImagePath;
                existing.HasVariants = dto.HasVariants;
                existing.IsSynced = true;
                existing.LastSyncedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("variant")]
        public async Task<IActionResult> SyncVariant([FromBody] ProductVariantDto dto)
        {
            var existing = await _db.ProductVariant.IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Id == dto.Id);

            if (existing == null)
            {
                _db.ProductVariant.Add(new ProductVariant
                {
                    Id = dto.Id,
                    ProductId = dto.ProductId,
                    Name = dto.Name,
                    Sku = dto.Sku,
                    CostPrice = dto.CostPrice,
                    SalePrice = dto.SalePrice,
                    Quantity = dto.Quantity,
                    Quality = dto.Quality,
                    ImagePath = dto.ImagePath,
                    IsSynced = true,
                    LastSyncedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Name = dto.Name;
                existing.Sku = dto.Sku;
                existing.CostPrice = dto.CostPrice;
                existing.SalePrice = dto.SalePrice;
                existing.Quantity = dto.Quantity;
                existing.Quality = dto.Quality;
                existing.IsSynced = true;
                existing.LastSyncedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("sale")]
        public async Task<IActionResult> SyncSale([FromBody] SaleDto dto)
        {
            var existing = await _db.Sale.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (existing == null)
            {
                _db.Sale.Add(new Sale
                {
                    Id = dto.Id,
                    ProductId = dto.ProductId,
                    ProductVariantId = dto.ProductVariantId,
                    ProductName = dto.ProductName,
                    VariantName = dto.VariantName,
                    QuantitySold = dto.QuantitySold,
                    SalePriceAtTime = dto.SalePriceAtTime,
                    CostPriceAtTime = dto.CostPriceAtTime,
                    SoldAt = dto.SoldAt,
                    Notes = dto.Notes,
                    IsSynced = true,
                    LastSyncedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.IsSynced = true;
                existing.LastSyncedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("bill")]
        public async Task<IActionResult> SyncBill([FromBody] BillDto dto)
        {
            var existing = await _db.Bill.IgnoreQueryFilters()
                .Include(b => b.Items)
                .FirstOrDefaultAsync(b => b.Id == dto.Id);

            if (existing == null)
            {
                var bill = new Bill
                {
                    Id = dto.Id,
                    BillNo = dto.BillNo,
                    BilledAt = dto.BillDate,                              // BilledAt (not BillDate)
                    CustomerName = dto.CustomerName,
                    CustomerPhone = dto.CustomerPhone,
                    // CustomerAddress — does not exist on Bill entity, skipped
                    PaymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod), // string → enum
                    PaymentStatus = (PaymentStatus)dto.PaymentStatus,
                    SubTotal = dto.SubTotal,
                    DiscountAmount = dto.DiscountAmount,
                    // TaxAmount — does not exist on Bill entity, skipped
                    TotalAmount = dto.GrandTotal,                         // TotalAmount (not GrandTotal)
                    Notes = dto.Notes,
                    IsSynced = true,
                    LastSyncedAt = DateTime.UtcNow,
                    Items = dto.Items.Select(i => new BillItem
                    {
                        Id = i.Id,
                        BillId = dto.Id,
                        ProductId = i.ProductId,
                        ProductVariantId = i.ProductVariantId,
                        ProductName = i.ProductName,
                        VariantName = i.VariantName,
                        // Sku — does not exist on BillItem entity, skipped
                        UnitPrice = i.UnitPrice,
                        CostPrice = i.CostPriceAtTime,                    // CostPrice (not CostPriceAtTime)
                        Quantity = i.Quantity,
                        IsSynced = true,
                        LastSyncedAt = DateTime.UtcNow
                    }).ToList()
                };

                _db.Bill.Add(bill);
            }
            else
            {
                // Don't overwrite historical bill data — just mark as synced
                existing.IsSynced = true;
                existing.LastSyncedAt = DateTime.UtcNow;
                foreach (var item in existing.Items)
                {
                    item.IsSynced = true;
                    item.LastSyncedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}