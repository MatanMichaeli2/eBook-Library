using System;

namespace WebApplication2.Helpers
{
    public interface IPaymentService
    {
        PaymentResult ProcessPayment(string paymentMethod, decimal amount);
    }

    public class PaymentService : IPaymentService
    {
        public PaymentResult ProcessPayment(string paymentMethod, decimal amount)
        {
            // Simulate payment processing
            try
            {
                if (string.IsNullOrEmpty(paymentMethod))
                {
                    return new PaymentResult { IsSuccess = false, Message = "Payment method is required." };
                }

                // Example: Simulate PayPal or Credit Card Payment
                if (paymentMethod.Equals("PayPal", StringComparison.OrdinalIgnoreCase))
                {
                    // Simulate PayPal processing
                    return new PaymentResult { IsSuccess = true, Message = "Payment processed via PayPal." };
                }
                else if (paymentMethod.Equals("CreditCard", StringComparison.OrdinalIgnoreCase))
                {
                    // Simulate credit card processing
                    return new PaymentResult { IsSuccess = true, Message = "Payment processed via Credit Card." };
                }
                else
                {
                    return new PaymentResult { IsSuccess = false, Message = "Unsupported payment method." };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResult { IsSuccess = false, Message = $"Payment failed: {ex.Message}" };
            }
        }
    }

    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
