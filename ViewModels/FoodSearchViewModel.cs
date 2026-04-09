using System.Collections.Generic;
using Turbo_Food_Main.Models;

namespace Turbo_Food_Main.ViewModels
{
    public class FoodSearchViewModel
    {
        public string? Query { get; set; }
        public string? Category { get; set; }
        public bool AvailableOnly { get; set; } = true;
        public string SortBy { get; set; } = "price_asc";
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public List<string> Categories { get; set; } = new();
        public List<MenuItem> Items { get; set; } = new();

        public List<CartLineViewModel> CartItems { get; set; } = new();
        public int CartTotalItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal GrandTotal { get; set; }
    }
}

