using System;

namespace Turbo_Food_Main.ViewModels
{
    public class UserRatingViewModel
    {
        public string ItemName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

