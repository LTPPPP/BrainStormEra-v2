using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
namespace DataAccessLayer.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? UserAddress { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? UserImage { get; set; }
        public string Role { get; set; } = "";
        public DateTime? CreatedAt { get; set; }

        // Bank Information
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? AccountHolderName { get; set; }

        // Statistics
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int InProgressCourses { get; set; }
        public int CertificatesEarned { get; set; }
        public int TotalAchievements { get; set; }
    }
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? UserAddress { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        // Bank Information
        [StringLength(50, ErrorMessage = "Bank account number cannot exceed 50 characters")]
        public string? BankAccountNumber { get; set; }

        [StringLength(255, ErrorMessage = "Bank name cannot exceed 255 characters")]
        public string? BankName { get; set; }

        [StringLength(255, ErrorMessage = "Account holder name cannot exceed 255 characters")]
        public string? AccountHolderName { get; set; }

        public IFormFile? ProfileImage { get; set; }

        public string? CurrentImagePath { get; set; }
    }
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Confirmation password does not match")]
        public string ConfirmPassword { get; set; } = "";
    }
}
