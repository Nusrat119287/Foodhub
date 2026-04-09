using System;
using System.ComponentModel.DataAnnotations;

namespace Turbo_Food_Main.Models
{
    public class UserRating
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public Users User { get; set; } = null!;
    }
}

