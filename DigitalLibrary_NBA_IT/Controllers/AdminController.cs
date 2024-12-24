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

        public ActionResult Details(int id)
        {
            var user = db.USERS.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
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
                book.Price = parsedPrice.ToString("0.00");
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
                Book_ID = newBookId.ToString().PadLeft(10), // יישור מספר הספר לעד 10 תווים
                Title = Title,
                Publish = Publish,
                Price = Price.ToString("0.00"),
                CopiesAvailable = CopiesAvailable.ToString(),
                ImageUrl = ImageUrl,
                age = age // הוספת מגבלת גיל

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

