using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;
using BCrypt.Net;


namespace DigitalLibrary_NBA_IT.Controllers
{
    public class AdminController : Controller
    {
        private Digital_library_DBEntities db = new Digital_library_DBEntities();

        // פעולה להצגת כל המשתמשים
        [HttpGet]
        public ActionResult ManageUsers()
        {
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                return RedirectToAction("Login", "User");
            }

            var users = db.USERS.ToList();
            return View(users);
        }

        // פעולה למחיקת משתמש
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int userId)
        {
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                return RedirectToAction("Login", "User");
            }

            var user = db.USERS.Find(userId);

            if (user != null)
            {
                db.USERS.Remove(user);
                db.SaveChanges();
                TempData["Message"] = "User deleted successfully.";
            }
            else
            {
                TempData["Message"] = "User not found.";
            }

            return RedirectToAction("ManageUsers");
        }

        // פעולה לעריכת משתמש (שלב הבא)
        [HttpGet]

        public ActionResult Details(int? id)
        {
            // בדיקת חיבור משתמש כמנהל
            if (Session["UserID"] == null || !(Session["IsAdmin"] is bool isAdmin && isAdmin))
            {
                TempData["Message"] = "You must be logged in as an admin to access this page.";
                return RedirectToAction("Login", "User");
            }

            // בדיקה אם `id` הוא null
            if (id == null)
            {
                TempData["Message"] = "Invalid user ID.";
                return RedirectToAction("ManageUsers", "Admin");
            }

            var user = db.USERS.Find(id);
            if (user == null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToAction("ManageUsers", "Admin");
            }

            // חישוב סטטיסטיקות
            var userLibrary = db.UserLibrary.Where(ul => ul.User_ID == id).ToList();

            ViewBag.TotalBooksPurchased = userLibrary.Count(ul => !ul.IsBorrowed);
            ViewBag.TotalBooksBorrowed = userLibrary.Count(ul => ul.IsBorrowed);
            ViewBag.TotalAmountSpent = userLibrary
                .Where(ul => !ul.IsBorrowed)
                .Sum(ul => decimal.TryParse(ul.Books.Price, out var price) ? price : 0);

            return View(user);
        }


        [HttpGet]
        public ActionResult ManageBooks(string query)
        {
            var books = db.Books.AsQueryable();

            // בדיקה אם השאילתה מכילה ערך והוספת תנאי חיפוש
            if (!string.IsNullOrEmpty(query))
            {
                books = books.Where(b => b.Title.Contains(query) || b.Publish.Contains(query));
                ViewBag.Query = query; // שמירת השאילתה עבור הצגת הערך בתיבת החיפוש
            }

            return View(books.ToList());
        }

        // פעולה לעדכון מחיר הספר
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePrice(int bookId, string newPrice)
        {
            var book = db.Books.FirstOrDefault(b => b.Book_ID == bookId.ToString());
            if (book != null && decimal.TryParse(newPrice, out decimal parsedPrice))
            {
                // אם המחיר החדש נמוך מהמחיר הנוכחי
                if (decimal.TryParse(book.Price, out decimal currentPrice) && parsedPrice < currentPrice)
                {
                    book.OriginalPrice = book.Price; // שמירת המחיר המקורי
                }

                book.Price = parsedPrice.ToString("0.00"); // עדכון המחיר החדש
                db.SaveChanges();
                TempData["Message"] = $"The price for {book.Title} was updated successfully.";
            }
            else
            {
                TempData["Message"] = "Failed to update the price. Please try again.";
            }

            return RedirectToAction("ManageBooks");
        }


        // פעולה למחיקת ספר
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBook(string bookId)
        {
            if (string.IsNullOrEmpty(bookId))
            {
                TempData["Error"] = "Invalid book ID.";
                return RedirectToAction("ManageBooks");
            }

            var book = db.Books.FirstOrDefault(b => b.Book_ID.Trim() == bookId.Trim());

            if (book != null)
            {
                db.Books.Remove(book);
                db.SaveChanges();
                TempData["Message"] = "Book deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Book not found.";
            }

            return RedirectToAction("ManageBooks");
        }


        // פעולה להצגת עמוד הוספת ספר
        [HttpGet]
        public ActionResult AddBookView()
        {
            return View();
        }

        // פעולה להוספת ספר חדש
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBook(string Title, string Publish, decimal Price, int CopiesAvailable, string ImageUrl, int age)
        {
            // מציאת Book_ID הגבוה ביותר בטבלה
            int maxBookId = db.Books.ToList().Select(b => int.TryParse(b.Book_ID.Trim(), out int id) ? id : 0).Max();
            int newBookId = maxBookId + 1;

            var book = new Books
            {
                Book_ID = newBookId.ToString(), // יישור מספר הספר לעד 10 תווים
                Title = Title,
                Publish = Publish,
                Price = Price.ToString("0.00"),
                CopiesAvailable = CopiesAvailable.ToString(),
                ImageUrl = ImageUrl,
                age = age, // הוספת מגבלת גיל
                OriginalPrice = null

            };

            try
            {
                db.Books.Add(book);
                db.SaveChanges();

                TempData["Message"] = "Book added successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message; // שמירת הודעת שגיאה אם ישנה בעיה
            }

            return RedirectToAction("ManageBooks");
        }


        // פעולה לעמוד דוחות מערכת
        public ActionResult SystemReports()
        {
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                return RedirectToAction("Login", "User");
            }

            // לוגיקה להפקת דוחות - בהמשך נוסיף את הלוגיקה
            return View();
        }

        // פעולה לעדכון מספר עותקים זמינים
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateCopies(string bookId, string newCopies)
        {
            if (string.IsNullOrEmpty(bookId) || string.IsNullOrEmpty(newCopies))
            {
                TempData["Error"] = "Invalid book ID or number of copies.";
                return RedirectToAction("ManageBooks");
            }

            var book = db.Books.FirstOrDefault(b => b.Book_ID.Trim() == bookId.Trim());
            if (book != null && int.TryParse(newCopies, out int parsedCopies))
            {
                book.CopiesAvailable = parsedCopies.ToString();
                db.SaveChanges();
                TempData["Message"] = $"The number of copies for {book.Title} was updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update the number of copies. Please try again.";
            }

            return RedirectToAction("ManageBooks");
        }

    }

}

