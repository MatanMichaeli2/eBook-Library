using WebApplication2.Data;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Helpers;

public class BorrowedBookService
{
    private readonly LibraryContext _context;
    private readonly EmailService _emailService;

    public BorrowedBookService(LibraryContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public void ProcessExpiredBorrows()
    {
        // Fetch all expired borrowed books
        var expiredBorrows = _context.BorrowedBooks
            .Include(b => b.Book)
            .Where(b => !b.IsReturned && b.ReturnDate <= DateTime.Now)
            .ToList();

        foreach (var borrow in expiredBorrows)
        {
            // Increment the book's available copies
            if (borrow.Book != null)
            {
                borrow.Book.CopiesAvailable++;
                Console.WriteLine($"Book '{borrow.Book.Title}' now has {borrow.Book.CopiesAvailable} copies available.");
            }

            // Remove the borrowed book entry
            _context.BorrowedBooks.Remove(borrow);

            Console.WriteLine($"Borrow record for BookId: {borrow.BookId}, UserId: {borrow.UserId} has been removed.");
        }

        // Save all changes to the database
        _context.SaveChanges();
    }

    public void SendReminderNotifications()
    {
        var now = DateTime.Now;
        var reminderDate = now.AddDays(5);

        // Fetch borrowed books where return date is 5 days away and reminder hasn't been sent
        var booksToRemind = _context.BorrowedBooks
            .Include(b => b.Book)
            .Include(b => b.User)
            .Where(b => !b.IsReturned && b.ReturnDate.Date == reminderDate.Date && !b.IsReminderSent)
            .ToList();

        foreach (var borrow in booksToRemind)
        {
            try
            {
                // Send reminder email
                _emailService.SendReminder(
                    toEmail: borrow.User.Email,
                    userName: borrow.User.FirstName,
                    bookTitle: borrow.Book.Title,
                    returnDate: borrow.ReturnDate
                );

                // Mark the reminder as sent
                borrow.IsReminderSent = true;

                Console.WriteLine($"Reminder sent to {borrow.User.Email} for book '{borrow.Book.Title}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reminder to {borrow.User.Email}: {ex.Message}");
            }
        }

        // Save changes to the database
        _context.SaveChanges();
    }

    public void ProcessWaitingList()
    {
        // Fetch books with available copies and users in the waiting list
        var waitingListEntries = _context.WaitingLists
            .Include(w => w.Book)
            .Include(w => w.User)
            .OrderBy(w => w.Position) 
            .ToList();

        // Group by BookId to process each book separately
        var groupedWaitingLists = waitingListEntries
            .GroupBy(w => w.BookId)
            .Where(g => g.First().Book.CopiesAvailable > 0);

        foreach (var bookGroup in groupedWaitingLists)
        {
            var book = bookGroup.First().Book;

            // Process users for this book while copies are available
            foreach (var waitingUser in bookGroup)
            {
                if (book.CopiesAvailable <= 0)
                    break; // Stop processing if no copies are available

                try
                {
                    // Send notification email
                    _emailService.SendNotification(
                        toEmail: waitingUser.User.Email,
                        userName: waitingUser.User.FirstName,
                        bookTitle: book.Title
                    );

                    // Remove user from waiting list
                    _context.WaitingLists.Remove(waitingUser);

                    // Decrement available copies
                    book.CopiesAvailable--;

                    Console.WriteLine($"Notification sent to {waitingUser.User.Email} for book '{book.Title}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify {waitingUser.User.Email}: {ex.Message}");
                    break; // Exit loop if notification fails
                }
            }
        }

        // Save changes to the database
        _context.SaveChanges();
    }


}

