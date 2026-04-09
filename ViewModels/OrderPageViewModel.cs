using System.Collections.Generic;

namespace Turbo_Food_Main.ViewModels
{
    public class OrderPageViewModel
    {
        public List<CartLineViewModel> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal GrandTotal { get; set; }
        public string? StatusMessage { get; set; }
    }
}

