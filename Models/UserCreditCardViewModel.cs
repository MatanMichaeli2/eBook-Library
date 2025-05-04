using System;

namespace WebApplication2.ViewModels
{
    public class UserCreditCardViewModel
    {
        // User fields
        public string ID { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }

        // Credit-card fields
        public string CardHolderName { get; set; }
        public string CardHolderID { get; set; }
        public string CardNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string CVC { get; set; }
    }
}
