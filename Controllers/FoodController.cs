using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Turbo_Food_Main.Data;
using Turbo_Food_Main.Models;
using Turbo_Food_Main.ViewModels;

namespace Turbo_Food_Main.Controllers
{
    [Authorize]
    public class FoodController : Controller
    {
        private const string CartSessionKey = "turbofood.cart";
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly IWebHostEnvironment _environment;

        public FoodController(AppDbContext context, UserManager<Users> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Search(string? q, string? category, bool availableOnly = true, string sortBy = "price_asc", decimal? minPrice = null, decimal? maxPrice = null)
        {
            await EnsureSeedMenuItemsAsync();

            var query = _context.MenuItems.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(m => m.Name.Contains(q) || m.Description.Contains(q) || m.Category.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(m => m.Category == category);
            }

            if (availableOnly)
            {
                query = query.Where(m => m.Availability);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(m => m.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(m => m.Price <= maxPrice.Value);
            }

            query = sortBy switch
            {
                "price_desc" => query.OrderByDescending(m => m.Price),
                "name_asc" => query.OrderBy(m => m.Name),
                _ => query.OrderBy(m => m.Price),
            };

            var items = await query.ToListAsync();
            var categories = await _context.MenuItems.AsNoTracking()
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var cartItems = await BuildCartItemsAsync();
            var totals = CalculateTotals(cartItems);

            var model = new FoodSearchViewModel
            {
                Query = q,
                Category = category,
                AvailableOnly = availableOnly,
                SortBy = sortBy,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Items = items,
                Categories = categories,
                CartItems = cartItems,
                CartTotalItems = cartItems.Sum(x => x.Quantity),
                Subtotal = totals.subtotal,
                Discount = totals.discount,
                DeliveryFee = totals.deliveryFee,
                GrandTotal = totals.total
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Ordering()
        {
            var cartItems = await BuildCartItemsAsync();
            var totals = CalculateTotals(cartItems);

            var model = new OrderPageViewModel
            {
                CartItems = cartItems,
                Subtotal = totals.subtotal,
                Discount = totals.discount,
                DeliveryFee = totals.deliveryFee,
                GrandTotal = totals.total,
                StatusMessage = TempData["OrderStatus"] as string
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Review()
        {
            await EnsureSeedMenuItemsAsync();

            var items = await _context.MenuItems.AsNoTracking()
                .Where(m => m.Availability)
                .OrderBy(m => m.Name)
                .ToListAsync();

            var reviews = await _context.UserRatings.AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .ToListAsync();

            var model = new FoodReviewPageViewModel
            {
                MenuItems = items,
                RecentReviews = reviews,
                StatusMessage = TempData["ReviewStatus"] as string
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int itemId, int quantity = 1, string? returnUrl = null)
        {
            var cart = ReadCart();
            if (cart.ContainsKey(itemId))
            {
                cart[itemId] += Math.Max(1, quantity);
            }
            else
            {
                cart[itemId] = Math.Max(1, quantity);
            }
            SaveCart(cart);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Search));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCart(int itemId, int quantity)
        {
            var cart = ReadCart();
            if (quantity <= 0)
            {
                cart.Remove(itemId);
            }
            else
            {
                cart[itemId] = quantity;
            }
            SaveCart(cart);
            return RedirectToAction(nameof(Ordering));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int itemId)
        {
            var cart = ReadCart();
            cart.Remove(itemId);
            SaveCart(cart);
            return RedirectToAction(nameof(Ordering));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string? specialInstructions)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = ReadCart();
            if (!cart.Any())
            {
                TempData["OrderStatus"] = "Your basket is empty. Add items before checkout.";
                return RedirectToAction(nameof(Ordering));
            }

            var itemIds = cart.Keys.ToList();
            var items = await _context.MenuItems.Where(m => itemIds.Contains(m.ItemId)).ToListAsync();
            if (!items.Any())
            {
                TempData["OrderStatus"] = "No valid items found in basket.";
                return RedirectToAction(nameof(Ordering));
            }

            var subtotal = items.Sum(i => i.Price * cart[i.ItemId]);
            var discount = subtotal > 1500m ? 250m : 0m;
            var deliveryFee = subtotal > 0m ? 34m : 0m;
            var total = subtotal - discount + deliveryFee;

            var order = new Order
            {
                UserID = user.Id,
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Status = "Placed",
                SpecialInstructions = specialInstructions
            };

            foreach (var menuItem in items)
            {
                var qty = cart[menuItem.ItemId];
                order.OrderItems.Add(new OrderItem
                {
                    MealID = menuItem.ItemId,
                    Quantity = qty,
                    UnitPrice = menuItem.Price,
                    TotalPrice = menuItem.Price * qty
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            SaveCart(new Dictionary<int, int>());
            TempData["OrderStatus"] = $"Order #{order.OrderID} placed successfully.";
            return RedirectToAction(nameof(Ordering));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMenuItem(string name, string description, string category, decimal price, IFormFile? imageFile, bool availability = true)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(category) || price <= 0)
            {
                TempData["OrderStatus"] = "Please provide valid menu item details.";
                return RedirectToAction(nameof(Search));
            }

            string? imagePath = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                imagePath = await SaveUploadedFileAsync(imageFile);
            }

            _context.MenuItems.Add(new MenuItem
            {
                Name = name.Trim(),
                Description = description.Trim(),
                Category = category.Trim(),
                Price = price,
                ImagePath = imagePath,
                Availability = availability,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["OrderStatus"] = "Menu item added. You can now see it in the food list.";
            return RedirectToAction(nameof(Search));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int menuItemId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewStatus"] = "Rating must be between 1 and 5.";
                return RedirectToAction(nameof(Review));
            }

            var menuItem = await _context.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.ItemId == menuItemId);
            if (menuItem == null)
            {
                TempData["ReviewStatus"] = "Food item not found.";
                return RedirectToAction(nameof(Review));
            }

            _context.UserRatings.Add(new UserRating
            {
                UserId = user.Id,
                ItemName = menuItem.Name,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["ReviewStatus"] = "Review submitted successfully.";
            return RedirectToAction(nameof(Review));
        }

        private Dictionary<int, int> ReadCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrWhiteSpace(cartJson))
            {
                return new Dictionary<int, int>();
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<int, int>>(cartJson) ?? new Dictionary<int, int>();
            }
            catch
            {
                return new Dictionary<int, int>();
            }
        }

        private void SaveCart(Dictionary<int, int> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        private async Task<List<CartLineViewModel>> BuildCartItemsAsync()
        {
            var cart = ReadCart();
            if (!cart.Any())
            {
                return new List<CartLineViewModel>();
            }

            var ids = cart.Keys.ToList();
            var items = await _context.MenuItems.AsNoTracking().Where(m => ids.Contains(m.ItemId)).ToListAsync();

            return items.Select(i => new CartLineViewModel
            {
                ItemId = i.ItemId,
                Name = i.Name,
                UnitPrice = i.Price,
                Quantity = cart.TryGetValue(i.ItemId, out var qty) ? qty : 0
            })
            .Where(x => x.Quantity > 0)
            .OrderBy(x => x.Name)
            .ToList();
        }

        private static (decimal subtotal, decimal discount, decimal deliveryFee, decimal total) CalculateTotals(List<CartLineViewModel> cartItems)
        {
            var subtotal = cartItems.Sum(x => x.LineTotal);
            var discount = subtotal > 1500m ? 250m : 0m;
            var deliveryFee = subtotal > 0m ? 34m : 0m;
            var total = subtotal - discount + deliveryFee;
            return (subtotal, discount, deliveryFee, total);
        }

        private async Task<string> SaveUploadedFileAsync(IFormFile imageFile)
        {
            var uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(uploadsRoot);

            var safeName = Path.GetFileNameWithoutExtension(imageFile.FileName);
            var extension = Path.GetExtension(imageFile.FileName);
            var uniqueName = $"{safeName}_{Guid.NewGuid():N}{extension}";
            var savePath = Path.Combine(uploadsRoot, uniqueName);

            await using var stream = new FileStream(savePath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            return $"/uploads/{uniqueName}";
        }

        private async Task EnsureSeedMenuItemsAsync()
        {
            if (await _context.MenuItems.AnyAsync())
            {
                return;
            }

            var seed = new List<MenuItem>
            {
                new() { Name = "Farm House Xtreme Pizza", Description = "Loaded pizza with vegetables and cheese.", Category = "Pizza", Price = 850m, Availability = true },
                new() { Name = "Royal Cheese Burger with Fries", Description = "Juicy burger with crispy fries.", Category = "Fast Food", Price = 231m, Availability = true },
                new() { Name = "Kacchi Biryani", Description = "Traditional aromatic biryani.", Category = "Kacchi", Price = 450m, Availability = true },
                new() { Name = "Chicken Kebabs", Description = "Smoked kebabs with house spices.", Category = "Kebabs", Price = 390m, Availability = true },
                new() { Name = "Fresh Garden Salad", Description = "Healthy mixed salad bowl.", Category = "Salads", Price = 220m, Availability = true },
                new() { Name = "Cold Coffee", Description = "Chilled coffee blend.", Category = "Cold drink", Price = 120m, Availability = true }
            };

            foreach (var item in seed)
            {
                item.CreatedAt = DateTime.UtcNow;
            }

            _context.MenuItems.AddRange(seed);
            await _context.SaveChangesAsync();
        }
    }
}

