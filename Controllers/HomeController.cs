using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WebApplication2.Models;
using WebApplication2.Helpers;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly EmailService _emailService;
        private readonly LibraryContext _dbContext;
        public HomeController(ILogger<HomeController> logger, EmailService emailService, LibraryContext dbContext)
        {
            _logger = logger;
            _emailService = emailService;
            _dbContext = dbContext;
        }

        public IActionResult Index()
        {
            var feedbacks = _dbContext.WebSiteFeedbacks
                .OrderByDescending(f => f.SubmittedOn)
                .Take(10)
                .ToList();
            ViewData["WebSiteFeedbacks"] = feedbacks;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "All fields are required.";
                return RedirectToAction("Contact");
            }

            try
            {
                string body = $@"
                    <p><strong>From:</strong> {name} ({email})</p>
                    <p><strong>Subject:</strong> {subject}</p>
                    <p><strong>Message:</strong></p>
                    <p>{message}</p>
                ";

                // Send email using EmailService
                _emailService.SendEmail(
                    toEmail: "matan2315@gmail.com", // Replace with your email
                    subject: $"Contact Us Form Submission: {subject}",
                    body: body
                );

                TempData["SuccessMessage"] = "Your message has been sent successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email from the Contact Us page.");
                TempData["ErrorMessage"] = "There was an error sending your message. Please try again later.";
            }

            return RedirectToAction("Contact");
        }

        [HttpPost]
        public IActionResult SubmitWebSiteFeedback(string name, int rating, string comment)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "All fields are required, and the rating must be between 1 and 5.";
                return RedirectToAction("Index");
            }

            var feedback = new WebSiteFeedback
            {
                Name = name,
                Rating = rating,
                Comment = comment
            };

            _dbContext.WebSiteFeedbacks.Add(feedback);
            _dbContext.SaveChanges();

            TempData["SuccessMessage"] = "Thank you for your feedback!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteWebSiteFeedback([FromBody] int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning($"Invalid feedback ID received: {id}");
                return Json(new { success = false, message = "Invalid feedback ID." });
            }

            var feedback = _dbContext.WebSiteFeedbacks.FirstOrDefault(f => f.Id == id);
            if (feedback == null)
            {
                _logger.LogWarning($"Feedback with ID {id} not found.");
                return Json(new { success = false, message = "Feedback not found." });
            }

            _dbContext.WebSiteFeedbacks.Remove(feedback);
            _dbContext.SaveChanges();

            _logger.LogInformation($"Feedback with ID {id} successfully deleted.");
            return Json(new { success = true });
        }

    }
}

