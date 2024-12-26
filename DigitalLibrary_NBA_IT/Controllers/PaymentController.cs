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
            if (!cart.Any())
            {
                TempData["Message"] = "Your cart is empty.";
                return RedirectToAction("Cart", "Home");
            }

            return View(cart);
        }

        // עיבוד תשלום
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayment(string CreditCardNumber, string ExpiryDate, string CVV, string IDNumber)
        {
            // אימות פרטי התשלום (סימולציה בלבד)
            if (string.IsNullOrWhiteSpace(CreditCardNumber) || string.IsNullOrWhiteSpace(ExpiryDate) || string.IsNullOrWhiteSpace(CVV) || string.IsNullOrWhiteSpace(IDNumber))
            {
                TempData["Message"] = "Payment failed. Invalid details provided.";
                return RedirectToAction("Payment");
            }

            // סימולציה להצלחה
            TempData["Message"] = "Payment successful! Thank you for your purchase.";
            ClearCart(); // מחיקת עגלת הקניות
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
