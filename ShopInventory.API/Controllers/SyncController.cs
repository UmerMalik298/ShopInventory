using Microsoft.AspNetCore.Mvc;
using ShopInventory.API.DTOs;
using ShopInventory.API.Interfaces;
using ShopInventory.Application.Interfaces;
using ShopInventory.Domain.Entities.Products;


namespace ShopInventory.API.Controllers
{
    

    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {

  
        private readonly IDbContext _db;
        public SyncController( IDbContext db)
        {
            _db = db;
        }

        [HttpPost("product")]
        public async Task<IActionResult> SyncProduct([FromBody] ProductDto dto)
        {
            var product = new Product
            {
                Id = dto.Id,
                Name = dto.Name,
                Sku = dto.Sku,
                CostPrice = dto.CostPrice,
                SalePrice = dto.SalePrice,
                CreatedAt = dto.CreatedAt,
                IsActive = true
            };

            _db.Product.Update(product);
            await _db.SaveChangesAsync();


            return Ok();
        }


    }
}
