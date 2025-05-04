using WebApplication2.Models;
using System;
public class PurchasedBook
    {
        public int PurchasedBookId { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }
        public DateTime PurchaseDate { get; set; }

        // Navigation property for User
        public User User { get; set; }

        // Navigation property for Book
        public Book Book { get; set; }
    }

