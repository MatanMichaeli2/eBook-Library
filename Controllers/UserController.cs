using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebApplication2.Data;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class UserController : Controller
    {
        private readonly LibraryContext _context;

        public UserController(LibraryContext context)
        {
            _context = context;
        }

        [Authorize]
        public IActionResult RateBook(int id)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction("PersonalLibrary", "User");
            }

            var feedback = new Feedback { BookId = book.BookId };
            return View(feedback);
        }

        [Authorize]
        [HttpPost]
        public IActionResult SubmitFeedback(Feedback feedback)
        {

                // Set the feedback date
                feedback.FeedbackDate = DateTime.Now;

                // Set the UserId from the current logged-in user
                feedback.UserId = User.Identity.Name;

                // Add feedback to the database
                _context.Feedbacks.Add(feedback);
                _context.SaveChanges();

                TempData["Message"] = "Thank you for your feedback!";
                return RedirectToAction("PersonalLibrary", "User");
        }

        public IActionResult ViewFeedbacks(int bookId)
        {
            // Fetch feedback for the specific book
            var feedbacks = _context.Feedbacks
                .Where(f => f.BookId == bookId)
                .Include(f => f.User)
                .ToList();

            // Fetch the book details
            var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction("BookGallery");
            }
            // Calculate average rating
            var averageRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 0;
            // Pass data to the view
            ViewData["BookTitle"] = book.Title;
            ViewData["AverageRating"] = averageRating;
            return View(feedbacks);
        }


        [Authorize]
        [HttpPost]
        public IActionResult AddToWaitingList(int bookId)
        {
            // Check if the book exists
            var book = _context.Books.FirstOrDefault(b => b.BookId == bookId);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction("BookGallery", "Library");
            }

            // Check if the user is already on the waiting list
            var userId = User.Identity.Name; 
            var existingEntry = _context.WaitingLists.FirstOrDefault(w => w.BookId == bookId && w.UserId == userId);

            if (existingEntry != null)
            {
                TempData["ErrorMessage"] = "You are already on the waiting list for this book.";
                return RedirectToAction("BookGallery", "Library");
            }

            // Add user to the waiting list
            var position = _context.WaitingLists.Count(w => w.BookId == bookId) + 1;

            var waitingListEntry = new WaitingList
            {
                BookId = bookId,
                UserId = userId,
                AddedDate = DateTime.Now,
                Position = position
            };

            _context.WaitingLists.Add(waitingListEntry);
            _context.SaveChanges();

            TempData["PopupMessage"] = $"You have been added to the waiting list for \"{book.Title}\".";
            return RedirectToAction("BookGallery", "Library");
        }

        [Authorize]
        [HttpPost]
        public IActionResult DeleteBook(int bookId)
        {
            // Parse the current user's ID from User.Identity.Name
            if (!int.TryParse(User.Identity.Name, out int userId))
            {
                TempData["ErrorMessage"] = "Invalid user ID.";
                return RedirectToAction("PersonalLibrary", "User");
            }

            string userIdString = userId.ToString();

            // Check if the book exists in the user's purchased books
            var purchasedBook = _context.PurchasedBooks
                .FirstOrDefault(pb => pb.BookId == bookId && pb.UserId == userIdString);

            if (purchasedBook == null)
            {
                TempData["ErrorMessage"] = "Book not found in your purchased books.";
                return RedirectToAction("PersonalLibrary", "User");
            }

            // Remove the book from the PurchasedBooks table
            _context.PurchasedBooks.Remove(purchasedBook);
            _context.SaveChanges();

            TempData["Message"] = "Book successfully removed from your library.";
            return RedirectToAction("PersonalLibrary", "User");
        }

        [Authorize]
        public IActionResult DownloadBook(int bookId, string format)
        {
            // Validate the format
            var validFormats = new[] { "epub", "fb2", "mobi", "pdf" };
            if (!validFormats.Contains(format.ToLower()))
            {
                TempData["ErrorMessage"] = "Invalid format selected.";
                return RedirectToAction("PersonalLibrary", "User");
            }

            // Construct the file path
            var fileName = $"Book.{format}";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "books", format.ToLower(), fileName);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "File not found.";
                return RedirectToAction("PersonalLibrary", "User");
            }

            // Return the file for download
            return PhysicalFile(filePath, "application/octet-stream", fileName);
        }


        // Display user's personal library
        public IActionResult PersonalLibrary()
        {
            var userId = User.Identity.Name;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Fetch user details (e.g., from database)
            var user = _context.Users.FirstOrDefault(u => u.ID == userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Set FirstName and LastName in ViewBag
            ViewBag.FirstName = user.FirstName;
            ViewBag.LastName = user.LastName;


            var borrowedBooks = _context.BorrowedBooks
                .Where(bb => bb.UserId == userId)
                .Select(bb => new LibraryBook
                {
                    Id = bb.BookId,
                    Title = bb.Book.Title,
                    Author = bb.Book.Author,
                    Publisher = bb.Book.Publisher,
                    YearPublished = bb.Book.YearPublished,
                    CoverImage = bb.Book.CoverImage,
                    IsBorrowed = true,
                    RemainingTime = bb.ReturnDate > DateTime.Now
                        ? $"{(bb.ReturnDate - DateTime.Now).Days} days remaining"
                        : "Expired"
                }).ToList();

            var purchasedBooks = _context.PurchasedBooks
                .Where(pb => pb.UserId == userId)
                .Select(pb => new LibraryBook
                {
                    Id = pb.BookId,
                    Title = pb.Book.Title,
                    Author = pb.Book.Author,
                    Publisher = pb.Book.Publisher,
                    YearPublished = pb.Book.YearPublished,
                    CoverImage = pb.Book.CoverImage,
                    IsBorrowed = false
                }).ToList();

            var libraryBooks = borrowedBooks.Concat(purchasedBooks).ToList();

            return View("~/Views/User/PersonalLibrary.cshtml", libraryBooks);
        }

        public IActionResult ReadBook(int id, int page = 1)
        {
            var book = _context.Books.FirstOrDefault(b => b.BookId == id);
            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction("PersonalLibrary");
            }

            // Use ViewBag to pass data
            ViewBag.BookId = book.BookId;
            ViewBag.Title = book.Title;
            ViewBag.Author = book.Author;
            ViewBag.Publisher = book.Publisher;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = 4;

            return View();
        }




    }
}
