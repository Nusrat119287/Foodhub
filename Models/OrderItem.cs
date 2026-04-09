using System.ComponentModel.DataAnnotations;

namespace Turbo_Food_Main.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }
        public int MealID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public Order Order { get; set; } = null!;
        public MenuItem Meal { get; set; } = null!;
    }
}

