namespace ShopInventory.Application.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }
        public bool HasVariants { get; set; } // ← add this
        public List<ProductVariantDto> Variants { get; set; } = new();
        public decimal? OldPrice { get; set; }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public int Quantity { get; set; } = 1;
        public string? ImagePath { get; set; }
        public bool HasVariants { get; set; } = false; // ← add this

        public decimal? OldPrice { get; set; }
    }
}