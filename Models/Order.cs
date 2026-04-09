using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Turbo_Food_Main.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public string UserID { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        [MaxLength(200)]
        public string? SpecialInstructions { get; set; }

        public Users User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

