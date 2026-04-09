namespace Turbo_Food_Main.ViewModels
{
    public class CartLineViewModel
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}

