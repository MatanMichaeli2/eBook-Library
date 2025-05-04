using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication2.Models
{
    public class CreditCard
    {
        [Key]
        public int CreditCardId { get; set; }

        [Required]
        [RegularExpression("^[a-zA-Z]+(?: [a-zA-Z]+)*$", ErrorMessage = "Cardholder name must contain only letters and spaces.")]
        [StringLength(100, ErrorMessage = "Cardholder name cannot exceed 100 characters.")]
        public string CardHolderName { get; set; }

        [Required]
        [RegularExpression("^\\d{9}$", ErrorMessage = "Cardholder ID must contain exactly 9 digits.")]
        public string CardHolderID { get; set; }

        [Required]
        [RegularExpression("^\\d{16}$", ErrorMessage = "Credit card number must contain exactly 16 digits.")]
        public string CardNumber { get; set; }

        [Required]
        [RegularExpression("^(0[1-9]|1[0-2])/(?:[0-9]{2})$", ErrorMessage = "Expiration date must be in MM/YY format.")]
        public string ExpiryDate { get; set; }

        [Required]
        [RegularExpression("^\\d{3}$", ErrorMessage = "CVV must contain exactly 3 digits.")]
        public string CVC { get; set; }

        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
    }
}
