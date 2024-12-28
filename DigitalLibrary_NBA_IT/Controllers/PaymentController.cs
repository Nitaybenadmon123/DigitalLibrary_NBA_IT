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
    }
}
