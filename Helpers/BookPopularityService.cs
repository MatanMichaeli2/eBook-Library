using System.Linq;
using WebApplication2.Data;
using WebApplication2.Models;

public class BookPopularityService
{
    private readonly LibraryContext _context;

    public BookPopularityService(LibraryContext context)
    {
        _context = context;
    }

    public void UpdatePopularBooks()
    {
        // 1. Get top 3 popular books based on PurchasedBooks
        var topPurchasedBookIds = _context.PurchasedBooks
            .GroupBy(p => p.BookId)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key)
            .ToList();

        // 2. Update "CopiesAvailable" to 0 for top 3 books
        var allBooks = _context.Books.ToList();
        foreach (var book in allBooks)
        {
            if (topPurchasedBookIds.Contains(book.BookId))
            {
                book.CopiesAvailable = 0; 
            }
            else
            {
                if (book.CopiesAvailable == 0) 
                {
                    book.CopiesAvailable = 3; 
                }
            }
        }

        // 3. Save changes to the database
        _context.SaveChanges();
    }
}
