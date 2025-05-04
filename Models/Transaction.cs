namespace WebApplication2.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        // Change UserId to string to match the User model's ID
        public string UserId { get; set; }

        public int BookId { get; set; }

        public DateTime TransactionDate { get; set; }

        public string PaymentMethod { get; set; }

        public decimal Amount { get; set; }

        public bool PaymentStatus { get; set; } // True for successful, False for failed

        // Navigation properties
        public Book Book { get; set; }
        public User User { get; set; }
    }
}
