using System;
using System.ComponentModel.DataAnnotations;

namespace Turbo_Food_Main.Models
{
    public class MenuItem
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public bool Availability { get; set; } = true;

        [Range(0.01, 999999)]
        public decimal Price { get; set; }

        [MaxLength(300)]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

