using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Turbo_Food_Main.Data;
using Turbo_Food_Main.Models;
using Turbo_Food_Main.ViewModels;

namespace Turbo_Food_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountApiController : ControllerBase
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountApiController(
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

        // POST: api/AccountApi/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new Users
            {
                FullName = model.Name,
                UserName = model.Email,
                NormalizedUserName = model.Email.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper()
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            await _userManager.AddToRoleAsync(user, "User");

            return Ok(new
            {
                message = "Registration successful.",
                user = new
                {
                    id = user.Id,
                    name = user.FullName,
                    email = user.Email
                }
            });
        }

        // POST: api/AccountApi/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found after login." });
            }

            return Ok(new
            {
                message = "Login successful.",
                user = new
                {
                    id = user.Id,
                    name = user.FullName,
                    email = user.Email
                }
            });
        }

        // GET: api/AccountApi/profile
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

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

            var response = new
            {
                name = user.FullName,
                email = user.Email,
                preferredCuisines,
                dietPreference = preference?.DietPreference,
                ratings = ratings.Select(r => new
                {
                    itemName = r.ItemName,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdAt = r.CreatedAt
                })
            };

            return Ok(response);
        }

        public class PreferencesDto
        {
            public string? PreferredCuisines { get; set; }
            public string? DietPreference { get; set; }
        }

        // POST: api/AccountApi/preferences
        [Authorize]
        [HttpPost("preferences")]
        public async Task<IActionResult> SavePreferences([FromBody] PreferencesDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            var cuisinesList = string.IsNullOrWhiteSpace(dto.PreferredCuisines)
                ? Array.Empty<string>()
                : dto.PreferredCuisines
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
            entity.DietPreference = string.IsNullOrWhiteSpace(dto.DietPreference) ? null : dto.DietPreference;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Preferences saved.",
                preferredCuisines = cuisinesList,
                dietPreference = entity.DietPreference
            });
        }

        public class RatingDto
        {
            public string ItemName { get; set; } = string.Empty;
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        // POST: api/AccountApi/ratings
        [Authorize]
        [HttpPost("ratings")]
        public async Task<IActionResult> AddRating([FromBody] RatingDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            if (string.IsNullOrWhiteSpace(dto.ItemName) || dto.Rating < 1 || dto.Rating > 5)
            {
                return BadRequest(new { message = "ItemName is required and Rating must be between 1 and 5." });
            }

            var entity = new UserRating
            {
                UserId = user.Id,
                ItemName = dto.ItemName.Trim(),
                Rating = dto.Rating,
                Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRatings.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Rating saved.",
                rating = new
                {
                    itemName = entity.ItemName,
                    rating = entity.Rating,
                    comment = entity.Comment,
                    createdAt = entity.CreatedAt
                }
            });
        }
    }
}

