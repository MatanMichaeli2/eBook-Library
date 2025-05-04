using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        public string Code { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!#$%^&*]).{6,}$",
            ErrorMessage = "Password must be at least 6 characters and include 1 uppercase letter, 1 number, and 1 special character (!#$%^&*).")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        public int Step { get; set; } = 1; // 1=email, 2=code, 3=new password
        public string Message { get; set; }
    }
}
