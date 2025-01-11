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
        // פעולה למחיקת משתמש
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int userId)
        {
            // בדיקת הרשאות אדמין
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                return RedirectToAction("Login", "User");
            }

            var user = db.USERS.Find(userId);

            if (user != null)
            {
                try
                {
                    // מחיקת רשומות מטבלת WAITLIST
                    var waitlistItems = db.WAITLIST.Where(w => w.User_ID == userId).ToList();
                    db.WAITLIST.RemoveRange(waitlistItems);
                    db.SaveChanges();

                    // מחיקת רשומות מטבלת UserLibrary
                    var userLibraryItems = db.UserLibrary.Where(ul => ul.User_ID == userId).ToList();
                    db.UserLibrary.RemoveRange(userLibraryItems);
                    db.SaveChanges();

                    // מחיקת רשומות מטבלת Reviews
                    var reviews = db.Reviews.Where(r => r.User_ID == userId).ToList();
                    db.Reviews.RemoveRange(reviews);
                    db.SaveChanges();

                    var reviewsSite = db.SiteFeedback.Where(u => u.User_ID == userId).ToList();
                    db.SiteFeedback.RemoveRange(reviewsSite);
                    db.SaveChanges();

                    // מחיקת המשתמש עצמו
                    db.USERS.Remove(user);
                    db.SaveChanges();

                    TempData["Message"] = "User and related data deleted successfully.";
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException?.Message ?? ex.Message;
                    TempData["Message"] = $"An error occurred while deleting the user: {errorMessage}";
                }
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
            if (Session["UserID"] == null || !(Session["IsAdmin"] is bool isAdmin && isAdmin))
            {
                TempData["Message"] = "You must be logged in as an admin to access this page.";
                return RedirectToAction("Login", "User");
            }
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
            if (Session["UserID"] == null || !(Session["IsAdmin"] is bool isAdmin && isAdmin))
            {
                TempData["Message"] = "You must be logged in as an admin to access this page.";
                return RedirectToAction("Login", "User");
            }
            var book = db.Books.FirstOrDefault(b => b.Book_ID == bookId.ToString());
            if (book != null && decimal.TryParse(newPrice, out decimal parsedPrice))
            {
                if (decimal.TryParse(book.Price, out decimal currentPrice) && parsedPrice < currentPrice)
                {
                    book.OriginalPrice = book.Price; // שמירת המחיר המקורי
                    book.DiscountStartDate = DateTime.Now; // שמירת תאריך תחילת המבצע
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

            if (Session["UserID"] == null || !(Session["IsAdmin"] is bool isAdmin && isAdmin))
            {
                TempData["Message"] = "You must be logged in as an admin to access this page.";
                return RedirectToAction("Login", "User");
            }
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
            if (Session["UserID"] == null || !(Session["IsAdmin"] is bool isAdmin && isAdmin))
            {
                TempData["Message"] = "You must be logged in as an admin to access this page.";
                return RedirectToAction("Login", "User");
            }

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
        ///////
        // פעולה להצגת רשימת ההמתנה
        [HttpGet]
        public ActionResult ManageWaitlist()
        {
            var waitlistEntries = db.WAITLIST.ToList();
            return View(waitlistEntries);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NotifyUser(int waitlistId, string message)
        {
            var emailService = new EmailService();
            try
            {
                var entry = db.WAITLIST.Find(waitlistId);

                if (entry == null)
                {
                    TempData["Message"] = "Waitlist entry not found.";
                    return RedirectToAction("ManageWaitlist");
                }

                // בדיקה אם למשתמש יש מייל תקין
                string userEmail = entry.USERS?.email; // Assuming USERS table has an email field
                if (string.IsNullOrEmpty(userEmail))
                {
                    TempData["Message"] = "User email not found.";
                    return RedirectToAction("ManageWaitlist");
                }

                // פרטי המייל
                string subject = $"Notification for '{entry.Books.Title}'";
                string body = $@"
            Dear {entry.USERS.name},<br/><br/>
            {message}<br/><br/>
            Thank you,<br/>
            Digital Library Team";

                try
                {
                    // שימוש בשירות שליחת מייל (יש להחליף את emailService בשירות שלך)
                    emailService.SendEmail(userEmail, subject, body);
                    TempData["Message"] = "Notification sent successfully.";
                }
                catch (Exception ex)
                {
                    // טיפול בשגיאה
                    Console.WriteLine($"Failed to send email to {userEmail}: {ex.Message}");
                    TempData["Message"] = $"Failed to send email: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"An error occurred: {ex.Message}";
            }

            // לאחר השלמת הפעולה, חזרה ל-View
            return RedirectToAction("ManageWaitlist");
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteWaitlistEntry(int waitlistId)
        {
            if (Session["UserID"] == null || !(Session["IsAdmin"] is bool isAdmin && isAdmin))
            {
                TempData["Message"] = "You must be logged in as an admin to access this page.";
                return RedirectToAction("Login", "User");
            }
            try
            {
                var entry = db.WAITLIST.Find(waitlistId);

                if (entry != null)
                {
                    db.WAITLIST.Remove(entry);
                    db.SaveChanges();
                    TempData["Message"] = "Waitlist entry deleted successfully.";

                    // החזרת ה-View עם הרשומות המעודכנות
                    var waitlistEntries = db.WAITLIST.ToList();
                    return View("ManageWaitlist", waitlistEntries);
                }

                TempData["Message"] = "Waitlist entry not found.";
                return RedirectToAction("ManageWaitlist");
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("ManageWaitlist");
            }
        }




        [HttpGet]
        public JsonResult GetWaitlistDetails()
        {

            try
            {
                var waitlistData = db.WAITLIST
                    .Join(
                        db.Books,
                        waitlistEntry => waitlistEntry.Book_ID,
                        book => book.Book_ID,
                        (waitlistEntry, book) => new
                        {
                            Waitlist_ID = waitlistEntry.ID,
                            User_ID = waitlistEntry.User_ID,
                            DateAdded = waitlistEntry.DateAdded,
                            BookTitle = book.Title
                        }
                    )
                    .Join(
                        db.USERS,
                        waitlistEntry => waitlistEntry.User_ID,
                        user => user.user_id,
                        (waitlistEntry, user) => new
                        {
                            Waitlist_ID = waitlistEntry.Waitlist_ID,
                            BookTitle = waitlistEntry.BookTitle,
                            UserName = user.name,
                            UserEmail = user.email,
                            DateAdded = waitlistEntry.DateAdded
                        }
                    )
                    .ToList();

                return Json(new { success = true, Waitlist = waitlistData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

    }

}

