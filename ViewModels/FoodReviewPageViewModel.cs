using System.Collections.Generic;
using Turbo_Food_Main.Models;

namespace Turbo_Food_Main.ViewModels
{
    public class FoodReviewPageViewModel
    {
        public List<MenuItem> MenuItems { get; set; } = new();
        public List<UserRating> RecentReviews { get; set; } = new();
        public string? StatusMessage { get; set; }
    }
}

