using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Turbo_Food_Main.Data;
using Turbo_Food_Main.Models;
using Turbo_Food_Main.ViewModels;

namespace Turbo_Food_Main.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        #region Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Profile", "Account");

            ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
            return View(model);
        }
        #endregion

        #region Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper()
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));

                await _userManager.AddToRoleAsync(user, "User");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Profile", "Account");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }
        #endregion

        [HttpGet]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }
            else
            {
                return RedirectToAction("ChangePassword", "Account", new { username = user.UserName });
            }
        }



        #region Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account"); // Redirect to login page
        }
        #endregion


        #region Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var preference = await _context.UserPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var ratings = await _context.UserRatings
                .AsNoTracking()
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var preferredCuisines = preference?.PreferredCuisines?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList() ?? new System.Collections.Generic.List<string>();

            var model = new ProfileViewModel
            {
                Name = user.FullName,
                Email = user.Email,
                PreferredCuisines = preferredCuisines,
                DietPreference = preference?.DietPreference,
                Ratings = ratings.Select(r => new UserRatingViewModel
                {
                    ItemName = r.ItemName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                StatusMessage = TempData["ProfileStatus"] as string
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePreferences(string? preferredCuisines, string? dietPreference)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var cuisinesList = string.IsNullOrWhiteSpace(preferredCuisines)
                ? Array.Empty<string>()
                : preferredCuisines
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            var entity = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (entity == null)
            {
                entity = new UserPreference
                {
                    UserId = user.Id
                };
                _context.UserPreferences.Add(entity);
            }

            entity.PreferredCuisines = string.Join(",", cuisinesList);
            entity.DietPreference = string.IsNullOrWhiteSpace(dietPreference) ? null : dietPreference;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["ProfileStatus"] = "Your food preferences have been saved.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRating(string itemName, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(itemName) || rating < 1 || rating > 5)
            {
                TempData["ProfileStatus"] = "Please provide a food item name and a rating between 1 and 5 stars.";
                return RedirectToAction(nameof(Profile));
            }

            var entity = new UserRating
            {
                UserId = user.Id,
                ItemName = itemName.Trim(),
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRatings.Add(entity);
            await _context.SaveChangesAsync();

            TempData["ProfileStatus"] = "Thank you! Your feedback has been saved.";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearRatings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var ratings = _context.UserRatings.Where(r => r.UserId == user.Id);
            _context.UserRatings.RemoveRange(ratings);
            await _context.SaveChangesAsync();

            TempData["ProfileStatus"] = "All your saved feedback has been cleared.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }

            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found!");
                return View(model);
            }

            var result = await _userManager.RemovePasswordAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                return RedirectToAction("Login", "Account");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(model);
            }
        }
        #endregion
    }
}
