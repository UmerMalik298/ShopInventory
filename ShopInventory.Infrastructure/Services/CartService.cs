using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Infrastructure.Services
{
    public class CartItem
    {
        public Guid ProductId { get; set; }
        public Guid? VariantId { get; set; }
        public string Name { get; set; } = "";
        public string? VariantName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class CartService
    {
        public List<CartItem> Items { get; private set; } = new();
        public event Action? OnCartChanged;

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(i =>
                i.ProductId == item.ProductId && i.VariantId == item.VariantId);

            if (existing != null)
                existing.Quantity++;
            else
                Items.Add(item);

            OnCartChanged?.Invoke();
        }

        public void Clear()
        {
            Items.Clear();
            OnCartChanged?.Invoke();
        }

        public int TotalItems => Items.Sum(i => i.Quantity);
    }
}
