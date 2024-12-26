using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class PaymentController : Controller
    {
        // עמוד תשלום
        [HttpGet]
        public ActionResult Payment()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            decimal totalAmount = cart.Sum(item =>
            {
                var priceString = item.Book.Price.Trim();
                decimal price = 0;
                if (!string.IsNullOrEmpty(priceString) && decimal.TryParse(priceString, out decimal parsedPrice))
                {
                    return item.Type == "borrow" ? parsedPrice / 4 : parsedPrice;
                }
                return price;
            });

            TempData["TotalAmount"] = totalAmount; // העברת סכום כולל ל-TempData
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayment(string CreditCardNumber, string ExpiryDate, string CVV, string IDNumber)
        {
            bool hasError = false;

            // Validate credit card number
            if (string.IsNullOrWhiteSpace(CreditCardNumber) || CreditCardNumber.Length != 16 || !CreditCardNumber.All(char.IsDigit))
            {
                TempData["CreditCardError"] = "Credit card number must be 16 digits.";
                hasError = true;
            }

            // Validate CVV
            if (string.IsNullOrWhiteSpace(CVV) || CVV.Length != 3 || !CVV.All(char.IsDigit))
            {
                TempData["CVVError"] = "CVV must be 3 digits.";
                hasError = true;
            }

            // Validate expiry date
            if (string.IsNullOrWhiteSpace(ExpiryDate))
            {
                TempData["ExpiryDateError"] = "Expiry date is required.";
                hasError = true;
            }

            // Validate ID number
            if (string.IsNullOrWhiteSpace(IDNumber) || IDNumber.Length != 9 || !IDNumber.All(char.IsDigit))
            {
                TempData["IDNumberError"] = "ID number must be exactly 9 digits.";
                hasError = true;
            }

            // אם יש טעויות, נבצע הפניה חזרה לדף התשלום
            if (hasError)
            {
                TempData["Message"] = "Payment failed. Please fix the errors and try again.";
                return RedirectToAction("Payment");
            }

            // במקרה שהכל תקין
            decimal totalAmount = TempData.ContainsKey("TotalAmount") ? (decimal)TempData["TotalAmount"] : 0;

            TempData["Message"] = $"Payment successful! Total: ${totalAmount:F2}.";
            ClearCart(); // Clear the cart after payment
            return RedirectToAction("Index", "Home");
        }






        private void ClearCart()
        {
            // מחיקת עגלת הקניות
            Session["Cart"] = null;
            Session["CartCount"] = 0;
        }
    }
}
