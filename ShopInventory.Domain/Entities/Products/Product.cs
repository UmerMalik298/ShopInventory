using ShopInventory.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Products
{
    public class Product : BaseEntity
    {
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public string? Barcode { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal? OldPrice { get; set; } = null;
        public string? Category { get; set; }         
        public string? Unit { get; set; }


        public int Quantity { get; set; } = 0;
        public string? ImagePath { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public bool HasVariants { get; set; } = false;

    
    }
}
