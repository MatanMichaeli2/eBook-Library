using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using WebApplication2.Data;
using WebApplication2.Models;
using WebApplication2.Helpers;

namespace WebApplication2.Controllers
{
    [Authorize]
    public class LibraryController : Controller
    {
        private readonly LibraryContext _context;
        private readonly PaymentService _paymentService;
        private readonly EmailService _emailService;

        public LibraryController(LibraryContext context, PaymentService paymentService, EmailService emailService)
        {
            _context = context;
            _paymentService = paymentService;
            _emailService = emailService;
        }
        [AllowAnonymous]
        public IActionResult BookGallery(string searchQuery, string genre, string ageLimit, string sortBy, bool? onSale)
        {
            var books = _context.Books.AsQueryable();

            // Calculate total number of books (before applying filters)
            ViewBag.TotalBooks = _context.Books.Count();

            // Search by title, author, or publisher
            if (!string.IsNullOrEmpty(searchQuery))
            {
                books = books.Where(b => b.Title.Contains(searchQuery) ||
                                         b.Author.Contains(searchQuery) ||
                                         b.Publisher.Contains(searchQuery));
            }

            // Filter by genre
            if (!string.IsNullOrEmpty(genre))
            {
                books = books.Where(b => b.Genre == genre);
            }

            // Filter by age limit
            if (!string.IsNullOrEmpty(ageLimit) && int.TryParse(ageLimit, out int limit))
            {
                books = books.Where(b => b.AgeLimit >= limit);
            }

            // Filter by on-sale books
            if (onSale.HasValue && onSale.Value)
            {
                books = books.Where(b => b.IsDiscounted && b.DiscountEndDate >= DateTime.Now);
            }

            // Sorting logic
            switch (sortBy)
            {
                case "priceLowHigh":
                    books = books.OrderBy(b => b.PriceBuy);
                    break;
                case "priceHighLow":
                    books = books.OrderByDescending(b => b.PriceBuy);
                    break;
                case "mostPopular":
                    books = books.OrderByDescending(b => b.Popularity);
                    break;
                case "yearPublished":
                    books = books.OrderByDescending(b => b.YearPublished);
                    break;
            }

            return View(books.ToList());
        }

        [HttpGet]
        public JsonResult GetTotalBooksCount()
        {
            var totalBooks = _context.Books.Count();
            return Json(new { totalBooks });
        }


        // Add to cart
        [HttpPost]
        public IActionResult AddToCart(int bookId, bool isBorrow)
        {
            var userId = User.Identity.Name;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var book = _context.Books.SingleOrDefault(b => b.BookId == bookId);

            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction("BookDetails", new { id = bookId });
            }

            if (isBorrow && book.CopiesAvailable <= 0)
            {
                TempData["Error"] = $"The book \"{book.Title}\" is currently unavailable for borrowing.";
                return RedirectToAction("BookGallery", "User");
            }

            var cart = GetCartFromSession();

            // Check if the book is already in the cart
            var existingItem = cart.FirstOrDefault(c => c.BookId == bookId && c.IsBorrow == isBorrow);
            if (existingItem != null)
            {
                TempData["Message"] = $"The book \"{book.Title}\" is already in your cart.";
            }
            else
            {
                cart.Add(new CartItem
                {
                    BookId = bookId,
                    Title = book.Title,
                    Price = isBorrow ? book.PriceBorrow : book.PriceBuy,
                    IsBorrow = isBorrow
                });

                SaveCartToSession(cart);

                TempData["Message"] = $"The book \"{book.Title}\" has been added to your cart.";
            }

            return RedirectToAction("Cart");
        }

        // View cart
        public IActionResult Cart()
        {
            var cart = GetCartFromSession();
            return View("~/Views/User/Cart.cshtml", cart);
        }

        // Remove from cart
        [HttpPost]
        public IActionResult RemoveFromCart(int bookId)
        {
            var cart = GetCartFromSession();

            cart.RemoveAll(item => item.BookId == bookId);

            SaveCartToSession(cart);

            TempData["Message"] = "Book removed from cart.";
            return RedirectToAction("Cart");
        }

        // Checkout
        [HttpPost]
        public IActionResult Checkout(string paymentMethod)
        {
            var userId = User.Identity.Name;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Fetch the user's email based on their ID
            var userEmail = _context.Users
                .Where(u => u.ID == userId)
                .Select(u => u.Email)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Unable to fetch your email address. Please contact support.";
                return RedirectToAction("Cart");
            }

            var cart = GetCartFromSession();

            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            var totalAmount = cart.Sum(item => item.Price);

            var paymentResult = _paymentService.ProcessPayment(paymentMethod, totalAmount);

            if (!paymentResult.IsSuccess)
            {
                TempData["Error"] = "Payment failed. Please try again.";
                return RedirectToAction("Cart");
            }

            foreach (var item in cart)
            {
                if (item.IsBorrow)
                {
                    _context.BorrowedBooks.Add(new BorrowedBook
                    {
                        UserId = userId,
                        BookId = item.BookId,
                        BorrowDate = DateTime.Now,
                        ReturnDate = DateTime.Now.AddDays(30),
                        IsReturned = false
                    });

                    var book = _context.Books.SingleOrDefault(b => b.BookId == item.BookId);
                    if (book != null)
                    {
                        book.CopiesAvailable--;
                    }
                }
                else
                {
                    _context.PurchasedBooks.Add(new PurchasedBook
                    {
                        UserId = userId,
                        BookId = item.BookId,
                        PurchaseDate = DateTime.Now
                    });
                }
            }

            _context.SaveChanges();

            // Debugging: Log the user email
            Console.WriteLine($"Sending email to: {userEmail}");

            // Send email notification to the user's email address
            _emailService.SendEmailNotification(userEmail, "Payment Confirmation", "Your payment was successful.");

            ClearCartSession();

            TempData["Message"] = "Payment successful. Enjoy your books!";
            return RedirectToAction("PersonalLibrary");
        }

        private List<CartItem> GetCartFromSession()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return cart;
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        private void ClearCartSession()
        {
            HttpContext.Session.Remove("Cart");
        }


    }
}
