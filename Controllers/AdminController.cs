using Microsoft.AspNetCore.Mvc;
using WebApplication2.Data;
using WebApplication2.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WebApplication2.ViewModels;

namespace WebApplication2.Controllers
{
    public class AdminController : Controller
    {
        private readonly LibraryContext _context;

        public AdminController(LibraryContext context)
        {
            _context = context;
        }

        // Admin panel menu
        public IActionResult AdminPanel()
        {
            return View();
        }

        // Display the AddBook form
        public IActionResult AddBook()
        {
            return View();
        }

        // Handle the form submission for adding a book
        [HttpPost]
        public IActionResult AddBook(Book book, string[] Formats)
        {
            if (Formats != null)
            {
                book.Formats = string.Join(",", Formats);
            }

            // Calculate the next BookId based on the highest existing ID
            var lastBookId = _context.Books.Any() ? _context.Books.Max(b => b.BookId) : 0;
            book.BookId = lastBookId + 1;

            // Check if DiscountEndDate is provided
            if (book.DiscountEndDate.HasValue)
            {
                // Ensure DiscountEndDate is not smaller than today
                if (book.DiscountEndDate.Value < DateTime.Now.Date)
                {
                    ModelState.AddModelError("DiscountEndDate", "Discount end date cannot be earlier than today.");
                }
                // Ensure DiscountEndDate is not more than 7 days from today
                else if (book.DiscountEndDate.Value > DateTime.Now.AddDays(7))
                {
                    ModelState.AddModelError("DiscountEndDate", "Discount end date cannot be more than 7 days from today.");
                }
            }


            if (ModelState.IsValid)
            {
                _context.Books.Add(book);
                _context.SaveChanges();
                return RedirectToAction("ManageBooks");
            }

            return View(book);
        }


        // Manage books
        public IActionResult ManageBooks()
        {
            var books = _context.Books.ToList();
            return View(books);
        }

        // Delete a book with confirmation
        [HttpPost]
        public IActionResult DeleteBook(int id)
        {
            try
            {
                var book = _context.Books.Find(id);
                if (book == null)
                {
                    return NotFound();
                }

                _context.Books.Remove(book);
                _context.SaveChanges();

                TempData["Message"] = $"The book \"{book.Title}\" has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting the book: {ex.Message}";
            }

            return RedirectToAction("ManageBooks");
        }

        // Display the EditBook form
        public IActionResult EditBook(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // Handle the form submission for editing a book
        [HttpPost]
        public IActionResult EditBook(Book book)
        {
            if (ModelState.IsValid)
            {
                var existingBook = _context.Books.Find(book.BookId);
                if (existingBook == null)
                {
                    TempData["ErrorMessage"] = "Book not found.";
                    return RedirectToAction("ManageBooks");
                }

                // Update only editable fields
                string format = existingBook.Formats;
                existingBook.Title = book.Title;
                existingBook.Author = book.Author;
                existingBook.Publisher = book.Publisher;
                existingBook.YearPublished = book.YearPublished;
                existingBook.Genre = book.Genre;
                existingBook.PriceBuy = book.PriceBuy;
                existingBook.PriceBorrow = book.PriceBorrow;
                existingBook.CopiesAvailable = book.CopiesAvailable;
                existingBook.AgeLimit = book.AgeLimit;
                existingBook.CoverImage = book.CoverImage;
                

                _context.Books.Update(existingBook);
                _context.SaveChanges();

                TempData["Message"] = $"The book \"{book.Title}\" has been updated successfully.";
                return RedirectToAction("ManageBooks");
            }

            TempData["ErrorMessage"] = "Invalid form submission.";
            return View(book);
        }

        // Manage users
        public IActionResult ManageUsers()
        {
            var model = (
                from u in _context.Users
                join cc in _context.CreditCards
                    on u.ID equals cc.UserId
                    into cards      // group-join → users with zero or many cards
                from card in cards.DefaultIfEmpty()   // left-join: card == null if none
                select new UserCreditCardViewModel
                {
                    ID = u.ID,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    CardHolderName = card != null ? card.CardHolderName : "",
                    CardHolderID = card != null ? card.CardHolderID : "",
                    CardNumber = card != null ? card.CardNumber : "",
                    ExpiryDate = card != null ? card.ExpiryDate : "",
                    CVC = card != null ? card.CVC : ""
                }
            ).ToList();

            return View(model);
        }




        // Add a method to manage roles, such as promoting/demoting a user
        [HttpPost]
        public IActionResult ChangeUserRole(string id, string newRole)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Role = newRole;
            _context.SaveChanges();

            TempData["Message"] = $"User \"{user.Email}\" role updated to \"{newRole}\" successfully.";
            return RedirectToAction("ManageUsers");
        }


        [HttpPost]
        public IActionResult DeleteUser(string id)
        {
            var user = _context.Users.FirstOrDefault(u => u.ID == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"User \"{user.FirstName} {user.LastName}\" has been successfully deleted.";
            return RedirectToAction("ManageUsers");
        }


        // Display the EditUser form
        public IActionResult EditUser(string id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // Handle the form submission for editing a user
        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Update(user);
                _context.SaveChanges();

                TempData["Message"] = $"User \"{user.FirstName} {user.LastName}\" has been updated successfully.";
                return RedirectToAction("ManageUsers");
            }

            return View(user);
        }

        // Display the EditDiscount form
        public IActionResult EditDiscount(int id)
        {
            var book = _context.Books.Find(id);
            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // Handle the form submission for editing a discount
        [HttpPost]
        public IActionResult EditDiscount(int id, decimal? discountedPrice, DateTime? discountEndDate)
        {
            var book = _context.Books.Find(id);
            if (book == null)
            {
                ModelState.AddModelError("", "Book not found.");
                return View(book);
            }

            if (discountedPrice.HasValue && discountEndDate.HasValue)
            {
                // Validate DiscountEndDate
                if (discountEndDate.Value < DateTime.Now.Date)
                {
                    ModelState.AddModelError("DiscountEndDate", "Discount end date cannot be earlier than today.");
                }
                else if (discountEndDate.Value > DateTime.Now.AddDays(7))
                {
                    ModelState.AddModelError("DiscountEndDate", "Discount end date must be within the next 7 days.");
                }

                // Validate DiscountedPrice
                if (discountedPrice.Value <= 0)
                {
                    ModelState.AddModelError("DiscountedPrice", "Discounted price must be greater than zero.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(book); // Return the view with validation errors
            }

            // Update the discount information
            if (discountedPrice.HasValue && discountEndDate.HasValue)
            {
                book.DiscountedPrice = discountedPrice.Value;
                book.DiscountEndDate = discountEndDate.Value;
            }
            else
            {
                // Remove discount
                book.DiscountedPrice = null;
                book.DiscountEndDate = null;
            }

            _context.Books.Update(book);
            _context.SaveChanges();

            return RedirectToAction("ManageBooks");
        }

        // Display the ManageWaitingList page
        public IActionResult ManageWaitingList()
        {
            var waitingList = _context.WaitingLists
                .Include(w => w.Book)
                .Include(w => w.User)
                .OrderBy(w => w.Position)
                .ToList();

            return View(waitingList);
        }


        // Handle deletion of a user from the WaitingList
        [HttpPost]
        public IActionResult DeleteFromWaitingList(int id)
        {
            var entry = _context.WaitingLists.Find(id);
            if (entry == null)
            {
                TempData["ErrorMessage"] = "Waiting list entry not found.";
                return RedirectToAction("ManageWaitingList");
            }

            _context.WaitingLists.Remove(entry);
            _context.SaveChanges();

            TempData["Message"] = "User successfully removed from the waiting list.";
            return RedirectToAction("ManageWaitingList");
        }




    }
}
