using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebApplication2.Helpers;
using WebApplication2.Models;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using System.Net.Mail;
using System.Text;

namespace WebApplication2.Controllers
{
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";
        private readonly EmailService _emailService;
        private readonly LibraryContext _context;

        public CartController(EmailService emailService, LibraryContext context)
        {
            _emailService = emailService;
            _context = context;
        }

        // Get shopping cart from session
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }
            return JsonConvert.DeserializeObject<List<CartItem>>(cartJson);
        }


        // Add item to cart
        [HttpPost]
        public IActionResult AddToCart(int bookId, string title, decimal priceBuy, decimal priceBorrow, bool isBorrow)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            var book = _context.Books.SingleOrDefault(b => b.BookId == bookId);

            if (book == null)
            {
                TempData["ErrorMessage"] = "Book not found.";
                return RedirectToAction("BookGallery", "Library");
            }

            // Check if the book has available copies if borrowing
            if (isBorrow && book.CopiesAvailable <= 0)
            {
                TempData["ErrorMessage"] = $"The book \"{title}\" is currently unavailable for borrowing.";
                return RedirectToAction("BookGallery", "Library");
            }

            // Check if the book is already in the cart
            var existingItem = cart.FirstOrDefault(c => c.BookId == bookId);
            if (existingItem != null)
            {
                existingItem.Price = isBorrow ? priceBorrow : priceBuy;
                existingItem.IsBorrow = isBorrow;
            }
            else
            {
                cart.Add(new CartItem
                {
                    BookId = bookId,
                    Title = title,
                    Price = isBorrow ? priceBorrow : priceBuy,
                    PriceBuy = priceBuy,
                    PriceBorrow = priceBorrow,
                    IsBorrow = isBorrow,
                    CoverImage = book.CoverImage
                });
            }

            SaveCart(cart);
            TempData["PopupMessage"] = $"{title} has been added to your cart ({(isBorrow ? "Borrow" : "Purchase")}).";
            return RedirectToAction("BookGallery", "Library");
        }

        // View cart
        public IActionResult ViewCart()
        {
            var cart = GetCart();
            ViewData["CartCount"] = cart.Count;
            return View(cart);
        }

        // Remove item from cart
        [HttpPost]
        public IActionResult RemoveFromCart(int bookId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.BookId == bookId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            TempData["Message"] = "Item removed from cart.";
            return RedirectToAction("ViewCart");
        }

        // Checkout
        public IActionResult Checkout()
        {
            var userId = User.Identity.Name;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var cart = GetCart();

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("ViewCart");
            }

            var borrowedBooksCount = _context.BorrowedBooks.Count(bb => bb.UserId == userId && !bb.IsReturned);
            var requestedBorrowCount = cart.Count(item => item.IsBorrow);

            if (borrowedBooksCount + requestedBorrowCount > 3)
            {
                TempData["ErrorMessage"] = $"You can only borrow up to 3 books at a time. You already have {borrowedBooksCount} borrowed books.";
                return RedirectToAction("ViewCart");
            }

            return View(cart);
        }

        [HttpPost]
        public IActionResult ProcessPayment(
            string paymentMethod,
            string creditCardNumber = null,
            string creditCardExpDate = null,
            string creditCardOwnerName = null,
            string cardHolderID = null,    // ← renamed
            string creditCardCVV = null
        )
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["PopupErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Checkout");
            }

            // 1. Persist the credit‐card details (unencrypted)
            var creditCard = new CreditCard
            {
                CardHolderName = creditCardOwnerName,
                CardHolderID = cardHolderID,       // ← use cardHolderID
                CardNumber = creditCardNumber,
                ExpiryDate = creditCardExpDate,
                CVC = creditCardCVV,
                UserId = User.Identity.Name
            };
            _context.CreditCards.Add(creditCard);
            _context.SaveChanges();    // now CardHolderID will be non-null

            // 2. Validate availability of books for borrowing
            foreach (var item in cart.Where(c => c.IsBorrow))
            {
                using (var tx = _context.Database.BeginTransaction())
                {
                    try
                    {
                        var book = _context.Books.FirstOrDefault(b => b.BookId == item.BookId);
                        if (book == null)
                        {
                            TempData["PopupErrorMessage"] = $"The book \"{item.Title}\" no longer exists.";
                            return RedirectToAction("Checkout");
                        }

                        if (book.CopiesAvailable > 0)
                        {
                            book.CopiesAvailable--;
                            _context.BorrowedBooks.Add(new BorrowedBook
                            {
                                UserId = User.Identity.Name,
                                BookId = item.BookId,
                                BorrowDate = DateTime.Now,
                                ReturnDate = DateTime.Now.AddDays(30),
                                IsReturned = false
                            });
                            TempData["PopupSuccessMessage"] = $"You have successfully borrowed \"{item.Title}\".";
                        }
                        else
                        {
                            var position = _context.WaitingLists.Count(w => w.BookId == item.BookId) + 1;
                            _context.WaitingLists.Add(new WaitingList
                            {
                                UserId = User.Identity.Name,
                                BookId = item.BookId,
                                AddedDate = DateTime.Now,
                                Position = position
                            });
                            TempData["PopupSuccessMessage"] = $"\"{item.Title}\" is unavailable. Added to waiting list.";
                        }

                        _context.SaveChanges();
                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine($"Error: {ex.Message}");
                        TempData["PopupErrorMessage"] = "Error processing your request. Please try again.";
                        return RedirectToAction("Checkout");
                    }
                }
            }

            // 3. Handle Purchased Books
            foreach (var item in cart.Where(c => !c.IsBorrow))
            {
                _context.PurchasedBooks.Add(new PurchasedBook
                {
                    UserId = User.Identity.Name,
                    BookId = item.BookId,
                    PurchaseDate = DateTime.Now
                });
            }
            _context.SaveChanges();

            // 4. Payment logic (Credit Card vs. PayPal)
            bool paymentSuccess = false;
            if (paymentMethod == "CreditCard")
            {
                paymentSuccess = ProcessCreditCardPayment(creditCardNumber, cart.Sum(i => i.Price));
                if (!paymentSuccess)
                {
                    TempData["PopupErrorMessage"] = "Payment failed. Please try again.";
                    return RedirectToAction("Checkout");
                }
            }
            else if (paymentMethod == "PayPal")
            {
                return Redirect(ProcessPayPalPayment(cart.Sum(i => i.Price)));
            }

            // 5. Send confirmation email
            var user = _context.Users.FirstOrDefault(u => u.ID == User.Identity.Name);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                var sb = new StringBuilder();
                sb.Append("<h1>Your order has been processed!</h1>")
                  .Append("<ul>");
                foreach (var item in cart)
                {
                    sb.Append($"<li>{item.Title} - {(item.IsBorrow ? "Borrowed" : "Purchased")} - {item.Price}$</li>");
                }
                sb.Append($"</ul><p><strong>Total: {cart.Sum(i => i.Price)}$</strong></p>");
                _emailService.SendEmailNotification(user.Email, "Order Confirmation", sb.ToString());
            }

            // 6. Clear cart & redirect
            HttpContext.Session.Remove(CartSessionKey);
            TempData["PopupSuccessMessage"] = "Payment successful! Enjoy your books!";
            return RedirectToAction("PaymentNotification");
        }



        // Intermediate notification page
        public IActionResult PaymentNotification()
        {
            return View();
        }


        private bool ProcessCreditCardPayment(string creditCardNumber, decimal amount)
        {
            
            return true;
        }

        private string ProcessPayPalPayment(decimal amount)
        {
            return $"https://www.sandbox.paypal.com/checkoutnow?amount={amount}";
        }

        [HttpPost]
        public IActionResult CompletePayment([FromBody] string paymentId)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["PopupErrorMessage"] = "The cart is empty.";
                return RedirectToAction("ViewCart");
            }

            try
            {
                // Process Borrowed Books
                foreach (var item in cart.Where(c => c.IsBorrow))
                {
                    var book = _context.Books.SingleOrDefault(b => b.BookId == item.BookId);
                    if (book != null && book.CopiesAvailable > 0)
                    {
                        book.CopiesAvailable--;

                        _context.BorrowedBooks.Add(new BorrowedBook
                        {
                            UserId = User.Identity.Name,
                            BookId = item.BookId,
                            BorrowDate = DateTime.Now,
                            ReturnDate = DateTime.Now.AddDays(30),
                            IsReturned = false
                        });
                    }
                }

                // Process Purchased Books
                foreach (var item in cart.Where(c => !c.IsBorrow))
                {
                    _context.PurchasedBooks.Add(new PurchasedBook
                    {
                        UserId = User.Identity.Name,
                        BookId = item.BookId,
                        PurchaseDate = DateTime.Now
                    });
                }

                // Save changes to the database
                _context.SaveChanges();

                // Clear the cart session
                HttpContext.Session.Remove(CartSessionKey);

                // Send email notification
                var user = _context.Users.FirstOrDefault(u => u.ID == User.Identity.Name);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var orderDetails = new StringBuilder();
                    orderDetails.Append("<h1>Your order has been processed!</h1>");
                    orderDetails.Append("<p>Thank you for your purchase. Here are the details of your order:</p>");
                    orderDetails.Append("<ul>");

                    foreach (var item in cart)
                    {
                        string type = item.IsBorrow ? "Borrowed" : "Purchased";
                        orderDetails.Append($"<li>{item.Title} - {type} - {item.Price}$</li>");
                    }

                    orderDetails.Append("</ul>");
                    orderDetails.Append($"<p><strong>Total: {cart.Sum(item => item.Price)}$</strong></p>");
                    orderDetails.Append("<p>We hope you enjoy your books!</p>");

                    string emailBody = orderDetails.ToString();
                    _emailService.SendEmailNotification(user.Email, "Order Confirmation", emailBody);
                }

                TempData["PopupSuccessMessage"] = "Payment successful! Your books have been added to your library.";
                return Ok("Payment processed successfully.");
            }
            catch (Exception ex)
            {
                TempData["PopupErrorMessage"] = "An error occurred while processing your payment.";
                Console.WriteLine($"Error: {ex.Message}");
                return RedirectToAction("ViewCart");
            }
        }


        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }

        [HttpPost]
        public IActionResult UpdateCartItem([FromBody] CartItemUpdateModel model)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.BookId == model.BookId);
            if (item != null)
            {
                item.IsBorrow = model.IsBorrow;
                item.Price = model.IsBorrow ? item.PriceBorrow : item.PriceBuy;
                SaveCart(cart); // Ensure the cart state is saved
            }
            return Ok();
        }

        public class CartItemUpdateModel
        {
            public int BookId { get; set; }
            public bool IsBorrow { get; set; }
        }

    }


}
