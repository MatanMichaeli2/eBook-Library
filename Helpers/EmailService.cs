using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using WebApplication2.Models;

namespace WebApplication2.Helpers
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Sends an email notification with optional CC and BCC.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Email body.</param>
        /// <param name="cc">CC email address (optional).</param>
        /// <param name="bcc">BCC email address (optional).</param>
        public void SendEmailNotification(string toEmail, string subject, string body, string cc = null, string bcc = null)
        {
            if (!IsValidEmail(toEmail))
            {
                throw new FormatException($"Invalid email address format: {toEmail}");
            }

            try
            {
                using var smtpClient = CreateSmtpClient();

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.Username),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                if (!string.IsNullOrWhiteSpace(cc) && IsValidEmail(cc))
                {
                    mailMessage.CC.Add(cc);
                }

                if (!string.IsNullOrWhiteSpace(bcc) && IsValidEmail(bcc))
                {
                    mailMessage.Bcc.Add(bcc);
                }

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sends an email without CC or BCC.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Email body.</param>
        public void SendEmail(string toEmail, string subject, string body)
        {
            if (!IsValidEmail(toEmail))
            {
                throw new FormatException($"Invalid email address format: {toEmail}");
            }

            try
            {
                using var smtpClient = CreateSmtpClient();

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.Username),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates an email address.
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates and configures an SMTP client using the provided email settings.
        /// </summary>
        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_emailSettings.Host)
            {
                Port = _emailSettings.Port,
                Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                EnableSsl = true,
            };
        }



        /// <summary>
        /// Sends a reminder email to the user for a borrowed book.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="bookTitle">Title of the borrowed book.</param>
        /// <param name="returnDate">Return date of the borrowed book.</param>
        public void SendReminder(string toEmail, string userName, string bookTitle, DateTime returnDate)
        {
            if (!IsValidEmail(toEmail))
            {
                throw new FormatException($"Invalid email address format: {toEmail}");
            }

            try
            {
                using var smtpClient = CreateSmtpClient();

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.Username),
                    Subject = "Reminder: Book Return Due Soon",
                    Body = $"Dear {userName},<br><br>" +
                           $"This is a friendly reminder that your borrowed book <b>\"{bookTitle}\"</b> is due for return on <b>{returnDate.ToShortDateString()}</b>.<br>" +
                           "Thank you for using our library service.<br><br>" +
                           "Best regards,<br>" +
                           "Library Team",
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);

                Console.WriteLine($"Reminder email sent to {toEmail} for book '{bookTitle}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending reminder email: {ex.Message}");
                throw;
            }
        }

        public void SendNotification(string toEmail, string userName, string bookTitle)
        {
            string subject = "Book Availability Notification";
            string body = $@"
        <h1>Good news, {userName}!</h1>
        <p>The book <strong>{bookTitle}</strong> is now available for you to borrow.</p>
        <p>Please log in to your account and complete the borrowing process as soon as possible.</p>";

            SendEmail(toEmail, subject, body);
        }

    }
}
