using System;
using System.ComponentModel.DataAnnotations;

namespace Turbo_Food_Main.Models
{
    public class UserPreference
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Comma-separated list of preferred cuisines for this user.
        /// </summary>
        [MaxLength(500)]
        public string PreferredCuisines { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DietPreference { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Users User { get; set; } = null!;
    }
}

