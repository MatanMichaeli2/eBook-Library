using WebApplication2.Models;

public class BorrowedBook
{
    public int BorrowedBookId { get; set; }
    public string UserId { get; set; }
    public int BookId { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public bool IsReturned { get; set; }
    public bool IsReminderSent { get; set; }


    // Navigation property for User
    public User User { get; set; }

    // Navigation property for Book
    public Book Book { get; set; }
}
