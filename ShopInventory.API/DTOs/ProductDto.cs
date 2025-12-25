namespace ShopInventory.API.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }


}
