using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Utilities;
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
                TempData["ErrorMessage"] = "Please correct the errors below and try again.";
                return View(model);
            }
            try
            {                // Check if username already exists
                bool usernameExists = await _context.Accounts.AnyAsync(a => a.Username == model.Username);
                if (usernameExists)
                {
                    TempData["ErrorMessage"] = "Username is already taken. Please choose a different username.";
                    ModelState.AddModelError("Username", "Username is already taken");
                    return View(model);
                }

                // Check if email already exists
                bool emailExists = await _context.Accounts.AnyAsync(a => a.UserEmail == model.Email);
                if (emailExists)
                {
                    TempData["ErrorMessage"] = "Email is already registered. Please use a different email or try logging in.";
                    ModelState.AddModelError("Email", "Email is already registered");
                    return View(model);
                }                // Create new account
                var account = new Account
                {
                    UserId = Guid.NewGuid().ToString(),
                    UserRole = "Learner", // Default role for new registrations
                    Username = model.Username,
                    PasswordHash = PasswordHasher.HashPassword(model.Password),
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
                await _context.SaveChangesAsync();                // Redirect to success page or login
                TempData["SuccessMessage"] = "Your account has been created successfully. You can now log in.";
                return RedirectToAction("Index", "Login");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while registering user");
                TempData["ErrorMessage"] = "A database error occurred while creating your account. Please try again.";
                ModelState.AddModelError(string.Empty, "An error occurred while registering your account. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                TempData["ErrorMessage"] = "An unexpected error occurred during registration. Please try again later.";
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later."); return View(model);
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
