using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class PaymentController : Controller
    {
        private Digital_library_DBEntities db = new Digital_library_DBEntities();

        // עמוד תשלום
        [HttpGet]
        public ActionResult Payment()
        {
            var totalAmount = CalculateTotalAmount();
            TempData["TotalAmount"] = totalAmount; // העברת סכום כולל ל-TempData
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayment(string CreditCardNumber, string ExpiryDate, string CVV, string IDNumber)
        {
            bool hasError = false;

            // Validate inputs (הקוד הקיים שלך)
            if (string.IsNullOrWhiteSpace(CreditCardNumber) || CreditCardNumber.Length != 16 || !CreditCardNumber.All(char.IsDigit))
            {
                TempData["CreditCardError"] = "Credit card number must be 16 digits.";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(CVV) || CVV.Length != 3 || !CVV.All(char.IsDigit))
            {
                TempData["CVVError"] = "CVV must be 3 digits.";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(ExpiryDate))
            {
                TempData["ExpiryDateError"] = "Expiry date is required.";
                hasError = true;
            }

            if (string.IsNullOrWhiteSpace(IDNumber) || IDNumber.Length != 9 || !IDNumber.All(char.IsDigit))
            {
                TempData["IDNumberError"] = "ID number must be exactly 9 digits.";
                hasError = true;
            }

            if (hasError)
            {
                TempData["Message"] = "Payment failed. Please fix the errors and try again.";
                return RedirectToAction("Payment");
            }

            // Retrieve user ID
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            int userId1 = GetCurrentUserId();

            // Count current borrowed books
            int currentBorrowedBooksCount = db.UserLibrary
                .Count(ul => ul.User_ID == userId1 && ul.IsBorrowed == true);

            // Count books to be borrowed in this purchase
            int booksToBorrowCount = cart.Count(item => item.Type == "borrow");

            // Check if the total borrowed books will exceed the limit
            if (currentBorrowedBooksCount + booksToBorrowCount > 3)
            {
                TempData["Message"] = "You cannot borrow more than 3 books at the same time. Please adjust your cart.";
                return RedirectToAction("Payment");
            }


            // Process payment and calculate total amount
            var totalAmount = CalculateTotalAmount();


            // Add purchases or borrows to UserLibrary table
            foreach (var item in cart)
            {
                var userId = GetCurrentUserId(); // וודא שהפונקציה מחזירה int

                var userLibraryEntry = new UserLibrary
                {
                    User_ID = userId, // מזהה המשתמש
                    Book_ID = item.Book.Book_ID, // מזהה הספר
                    PurchaseDate = DateTime.Now, // תאריך הרכישה
                    IsBorrowed = item.Type == "borrow", // true אם מושאל, false אם נרכש
                    ExpiryDate = item.Type == "borrow" ? DateTime.Now.AddDays(30) : (DateTime?)null // תאריך תפוגה
                };

                db.UserLibrary.Add(userLibraryEntry);// הוספת הרשומה לטבלה


                var book = db.Books.FirstOrDefault(b => b.Book_ID == item.Book.Book_ID); // שליפת הספר ממסד הנתונים
                if (book != null && int.TryParse(book.CopiesAvailable, out int copiesAvailable) && copiesAvailable > 0)
                {
                    book.CopiesAvailable = (copiesAvailable - 1).ToString(); // הפחתת הכמות הזמינה ב-1
                }
            }

            db.SaveChanges(); // שמירת השינויים במסד הנתונים
            //שליחת מייל למשתמש לאחר קנייה מוצלחת 
            try
            {
                var emailService = new EmailService();
                var userEmail = GetUserEmail(); // פונקציה לשליפת מייל המשתמש הנוכחי
                string subject = "Purchase Confirmation Digital Library";

                // בניית גוף המייל
                string body = "<h3>Thank you for your purchase!</h3>";
                body += "<p>Here are the details of your transaction:</p>";
                body += "<ul>";
                foreach (var item in cart)
                {
                    body += $"<li><strong>Book:</strong> {item.Book.Title} - {(item.Type == "borrow" ? "Borrowed" : "Purchased")}</li>";
                }
                body += "</ul>";
                body += $"<p><strong>Total Amount:</strong> ${totalAmount:F2}</p>";
                body += $"<p><strong>Date:</strong> {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}</p>";

                emailService.SendEmail(userEmail, subject, body);
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Payment successful, but failed to send confirmation email.";
            }

            // Simulate successful payment
            TempData["TotalAmount"] = totalAmount;
            TempData["Message"] = $"Payment successful! Total: ${totalAmount:F2}.";
            ClearCart(); // Clear the cart after payment

            return RedirectToAction("Index", "Home");
        }
        private int GetCurrentUserId()
        {
            // לדוגמה: אם מזהה המשתמש נשמר ב-Session
            return int.Parse(Session["UserId"].ToString());
        }


        private void ClearCart()
        {
            // מחיקת עגלת הקניות
            Session["Cart"] = null;
            Session["CartCount"] = 0;
        }

        private decimal CalculateTotalAmount()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            return cart.Sum(item =>
            {
                var priceString = item.Book.Price.Trim();
                decimal price = 0;
                if (!string.IsNullOrEmpty(priceString) && decimal.TryParse(priceString, out decimal parsedPrice))
                {
                    return item.Type == "borrow" ? parsedPrice / 4 : parsedPrice;
                }
                return price;
            });
        }

        private string GetUserEmail()
        {
            // שליפת מזהה המשתמש מתוך ה-Session
            int userId = GetCurrentUserId();

            // שליפת כתובת האימייל ממסד הנתונים
            var user = db.USERS.FirstOrDefault(u => u.user_id == userId);
            return user?.email ?? "guest@example.com"; // ברירת מחדל אם לא נמצא
        }

        public ActionResult PayWithPayPal(decimal totalAmount)
        {
            var paypalService = new PayPalService();

            // כתובות חזרה (URL)
            string returnUrl = Url.Action("PaymentSuccess", "Payment", null, Request.Url.Scheme);
            string cancelUrl = Url.Action("PaymentCancelled", "Payment", null, Request.Url.Scheme);

            // יצירת תשלום
            var payment = paypalService.CreatePayment(returnUrl, cancelUrl, totalAmount);

            // ניתוב ל-PayPal
            var approvalUrl = payment.links.FirstOrDefault(link => link.rel == "approval_url").href;
            return Redirect(approvalUrl);
        }

        public ActionResult PaymentSuccess(string paymentId, string token, string PayerID)
        {
            // טיפול בתשלום שהושלם
            TempData["Message"] = "Payment successful!";
            return RedirectToAction("Index", "Home");
        }

        public ActionResult PaymentCancelled()
        {
            // טיפול בתשלום שבוטל
            TempData["Message"] = "Payment cancelled.";
            return RedirectToAction("Index", "Home");
        }

    }
}
