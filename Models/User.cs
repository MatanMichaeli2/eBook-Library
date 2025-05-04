using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication2.Models
{
    public class User
    {

        [Key]
        [Required(ErrorMessage = "ID is required.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "ID must be exactly 9 characters long.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "ID must contain only digits.")]
        public string ID { get; set; }



        [Required(ErrorMessage = "First Name is required.")]
        [MaxLength(50, ErrorMessage = "First Name cannot exceed 50 characters.")]
        [MinLength(2,ErrorMessage = "First Name must be at least 2 characters long.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First Name can only contain letters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [MaxLength(50, ErrorMessage = "Last Name cannot exceed 50 characters.")]
        [MinLength(2, ErrorMessage = "Last Name must be at least 2 characters long.")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Last Name can only contain letters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Birth Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[!#$%^&*]).{6,}",
            ErrorMessage = "Password must have at least 6 characters, including 1 uppercase letter, 1 number, and 1 symbol.")]
        public string PasswordHash { get; set; }

        public string Role { get; set; } = "User";

        public ICollection<CreditCard> CreditCards { get; set; }
            = new List<CreditCard>();
    }
}