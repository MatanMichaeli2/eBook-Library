using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Data;
using WebApplication2.Helpers;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    public class AccountController : Controller
    {
        private readonly LibraryContext _context;
        private readonly EmailService _emailService;


        public AccountController(LibraryContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Registration form
        public IActionResult Register()
        {
            return View();
        }

        // Process registration
        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                // בדיקה אם יש משתמש עם אותו ID
                var existingUserById = _context.Users.SingleOrDefault(u => u.ID == user.ID);
                if (existingUserById != null)
                {
                    ModelState.AddModelError("ID", "A user with this ID already exists.");
                    return View(user);
                }

                // בדיקה אם יש משתמש עם אותו אימייל
                var existingUserByEmail = _context.Users.SingleOrDefault(u => u.Email == user.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    return View(user);
                }

                // Validate BirthDate (Ensure user is above a certain age, e.g., 13 years)
                if (user.BirthDate > DateTime.Now.AddYears(-13))
                {
                    ModelState.AddModelError("BirthDate", "You must be at least 13 years old to register.");
                    return View(user);
                }

                // Hash the password
                user.PasswordHash = ComputeSha256Hash(user.PasswordHash);

                // Assign default role
                user.Role = "User";

                // Save user to the database
                _context.Users.Add(user);
                _context.SaveChanges();

                // Redirect to login page
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // Login form
        public IActionResult Login()
        {
            return View();
        }

        // Process login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == email);
            if (user != null && user.PasswordHash == ComputeSha256Hash(password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.ID),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Invalid login credentials");
            return View();
        }


        // Process logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home"); 
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        private string GenerateResetCode()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }



        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            Console.WriteLine($"===> POST received with Step={model.Step}, Email='{model.Email}', Code='{model.Code}'");

            // STEP 1: Email submission
            if (model.Step == 1)
            {
                if (string.IsNullOrEmpty(model.Email))
                {
                    model.Message = "Email cannot be empty.";
                    return View(model);
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    model.Message = $"Email '{model.Email}' not found.";
                    return View(model);
                }

                string code = GenerateResetCode();
                HttpContext.Session.SetString("ResetCode", code);
                HttpContext.Session.SetString("ResetEmail", model.Email);

                Console.WriteLine($"===> Generated code '{code}' for email '{model.Email}'");
                _emailService.SendEmail(model.Email, "Reset Code", $"Your reset code is: {code}");

                var step2Model = new ForgotPasswordViewModel
                {
                    Step = 2,
                    Email = model.Email,
                    Message = "Code sent to your email."
                };

                return View(step2Model);
            }

            // STEP 2: Code verification
            else if (model.Step == 2)
            {
                Console.WriteLine($"===> Processing STEP 2");
                string sessionEmail = HttpContext.Session.GetString("ResetEmail");
                string sessionCode = HttpContext.Session.GetString("ResetCode");

                Console.WriteLine($"===> Session values: Email='{sessionEmail}', Code='{sessionCode}'");

                model.Email = sessionEmail;

                if (string.IsNullOrEmpty(model.Code))
                {
                    model.Message = "Please enter the verification code.";
                    return View(model);
                }

                if (!string.Equals(model.Code.Trim(), sessionCode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"===> Code mismatch: Expected '{sessionCode}', Got '{model.Code}'");
                    model.Message = "Invalid code.";
                    return View(model);
                }

                Console.WriteLine("===> Code verified successfully, moving to step 3");
                var step3Model = new ForgotPasswordViewModel
                {
                    Step = 3,
                    Email = sessionEmail,
                    Message = "Please enter your new password."
                };

                return View(step3Model);
            }

            // STEP 3: Password reset
            else if (model.Step == 3)
            {
                model.Email = HttpContext.Session.GetString("ResetEmail");
                Console.WriteLine($"===> Processing STEP 3 for email '{model.Email}'");

                // Debug validation issues
                ModelState.Remove(nameof(model.Code));
                ModelState.Remove(nameof(model.Message));

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("===> ModelState is invalid. Validation errors:");
                    foreach (var entry in ModelState)
                    {
                        foreach (var error in entry.Value.Errors)
                        {
                            Console.WriteLine($"===> Field: {entry.Key}, Error: {error.ErrorMessage}");
                        }
                    }

                    model.Step = 3;
                    model.Message = "Password validation failed. Please check the requirements below.";
                    return View(model);
                }

                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    model.Message = "User not found.";
                    return View(model);
                }

                user.PasswordHash = ComputeSha256Hash(model.NewPassword);
                _context.SaveChanges();

                HttpContext.Session.Remove("ResetCode");
                HttpContext.Session.Remove("ResetEmail");

                return RedirectToAction("Login", new { message = "Password reset successful." });
            }

            model.Message = "Something went wrong.";
            return View(model);
        }
    }
}
