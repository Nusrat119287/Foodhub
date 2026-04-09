using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Turbo_Food_Main.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress]
        public string Email { get; set; }

        public IList<string> PreferredCuisines { get; set; } = new List<string>();

        public string? DietPreference { get; set; }

        public IList<UserRatingViewModel> Ratings { get; set; } = new List<UserRatingViewModel>();

        public string? StatusMessage { get; set; }
    }
}
