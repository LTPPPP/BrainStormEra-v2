using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(255, ErrorMessage = "Username must be between 3 and 255 characters", MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and hyphens")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be between 6 and 100 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }

        [StringLength(255, ErrorMessage = "Full name must be less than 255 characters")]
        public string? FullName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Range(0, 2, ErrorMessage = "Invalid gender selection")]
        public short? Gender { get; set; } // 0: Other, 1: Male, 2: Female

        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Invalid phone number format")]
        [StringLength(15, ErrorMessage = "Phone number must be less than 15 characters")]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }

        [Required(ErrorMessage = "Please accept the terms and conditions")]
        public bool AcceptTerms { get; set; }
    }
}
