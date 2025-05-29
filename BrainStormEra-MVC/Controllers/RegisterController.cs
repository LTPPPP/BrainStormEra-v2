using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BrainStormEra_MVC.Controllers
{
    public class RegisterController : Controller
    {
        private readonly BrainStormEraContext _context;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(BrainStormEraContext context, ILogger<RegisterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            try
            {
                // Check if username already exists
                bool usernameExists = await _context.Accounts.AnyAsync(a => a.Username == model.Username);
                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "Username is already taken");
                    return View(model);
                }

                // Check if email already exists
                bool emailExists = await _context.Accounts.AnyAsync(a => a.UserEmail == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email is already registered");
                    return View(model);
                }

                // Create new account
                var account = new Account
                {
                    UserId = Guid.NewGuid().ToString(),
                    UserRole = "Student", // Default role for new registrations
                    Username = model.Username,
                    PasswordHash = HashPassword(model.Password),
                    UserEmail = model.Email,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null,
                    Gender = model.Gender,
                    PhoneNumber = model.PhoneNumber,
                    UserAddress = model.Address,
                    IsBanned = false,
                    AccountCreatedAt = DateTime.UtcNow,
                    AccountUpdatedAt = DateTime.UtcNow
                };

                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                // Redirect to success page or login
                TempData["RegistrationSuccess"] = "Your account has been created successfully. You can now log in.";
                return RedirectToAction("Index", "Login");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error occurred while registering user");
                ModelState.AddModelError(string.Empty, "An error occurred while registering your account. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Convert the byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashedBytes.Length; i++)
                {
                    builder.Append(hashedBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            if (string.IsNullOrEmpty(username) || !Regex.IsMatch(username, @"^[a-zA-Z0-9_-]+$"))
            {
                return Json(new { valid = false, message = "Invalid username format" });
            }

            bool exists = await _context.Accounts.AnyAsync(a => a.Username == username);
            return Json(new { valid = !exists, message = exists ? "Username is already taken" : "" });
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Json(new { valid = false, message = "Invalid email format" });
            }

            bool exists = await _context.Accounts.AnyAsync(a => a.UserEmail == email);
            return Json(new { valid = !exists, message = exists ? "Email is already registered" : "" });
        }
    }
}
