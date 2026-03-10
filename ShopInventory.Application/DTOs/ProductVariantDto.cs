namespace ShopInventory.Application.DTOs
{
    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Sku { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; }
        public string? Quality { get; set; }
        public string? ImagePath { get; set; }
    }

    public class CreateProductVariantDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Sku { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Quality { get; set; }
        public string? ImagePath { get; set; }
    }
}