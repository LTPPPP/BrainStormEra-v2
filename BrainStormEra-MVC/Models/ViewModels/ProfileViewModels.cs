using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_MVC.Models.ViewModels
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
        [Required(ErrorMessage = "Tên đầy đủ không được để trống")]
        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        public string? UserAddress { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        // Bank Information
        [StringLength(50, ErrorMessage = "Số tài khoản ngân hàng không được vượt quá 50 ký tự")]
        public string? BankAccountNumber { get; set; }

        [StringLength(255, ErrorMessage = "Tên ngân hàng không được vượt quá 255 ký tự")]
        public string? BankName { get; set; }

        [StringLength(255, ErrorMessage = "Tên chủ tài khoản không được vượt quá 255 ký tự")]
        public string? AccountHolderName { get; set; }

        public IFormFile? ProfileImage { get; set; }

        public string? CurrentImagePath { get; set; }
    }
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = "";
    }
}
