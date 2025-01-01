using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class HomeController : Controller
    {
        // חיבור למסד הנתונים
        private Digital_library_DBEntities db = new Digital_library_DBEntities();
        public JsonResult GetAuthors()
        {
            var authors = db.Authors
                .GroupBy(a => new { a.Name, a.Bio })
                .Select(g => new
                {
                    Name = g.Key.Name,
                    Bio = g.Key.Bio
                }).ToList();

            return Json(authors, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetBooksByAuthor(string authorName)
        {
            var books = db.Authors
                .Where(a => a.Name == authorName) // מסנן לפי שם המחבר
                .Select(a => a.Books) // מבצע נוויגציה לטבלת הספרים
                .SelectMany(book => db.Books.Where(b => b.Book_ID == book.Book_ID)) // מבטיח התאמה לספרים של המחבר בלבד
                .Distinct()
                .ToList();

            return Json(books, JsonRequestBehavior.AllowGet);
        }




        // פעולה ראשית להצגת הספרים וחיפוש
        public ActionResult Index(string query = "", string authorName = "")
        {
            var books = db.Books.AsQueryable();

            // חיפוש לפי שם הספר או המו"ל
            if (!string.IsNullOrEmpty(query))
            {
                books = books.Where(b => b.Title.Contains(query) || b.Publish.Contains(query));
            }

            // סינון לפי שם המחבר
            if (!string.IsNullOrEmpty(authorName))
            {
                books = books.Where(b => db.Authors.Any(a => a.Name == authorName && a.Book_ID == b.Book_ID));
            }

            ViewBag.Query = query; // שמירה על השאילתה בתיבת החיפוש
            ViewBag.AuthorName = authorName; // שמירה על שם המחבר בתיבת החיפוש

            return View(books.ToList());
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
        public ActionResult RemoveFromCart(string bookId)
        {
            // קבלת רשימת העגלה מה-Session
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // מציאת הספר לפי מזהה
            var itemToRemove = cart.FirstOrDefault(c => c.Book.Book_ID == bookId); // התאמת שם השדה
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove); // הסרת הספר מהעגלה
            }

            // שמירה חזרה ל-Session
            Session["Cart"] = cart;

            // עדכון ספירת הפריטים בעגלה
            Session["CartCount"] = cart.Count;

            // חזרה לעמוד העגלה
            return RedirectToAction("Cart");
        }
        private int GetCurrentUserID()
        {
            // לדוגמה: אם מזהה המשתמש נשמר ב-Session
            return int.Parse(Session["UserId"].ToString());
        }
        [HttpGet]
        public JsonResult CheckBorrowConditions(string id)
        {
            var userId = GetCurrentUserID();
            var book = db.Books.Find(id);

            if (book == null)
            {
                return Json(new { success = false, message = "Book not found." }, JsonRequestBehavior.AllowGet);
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // ספרים מושאלים בעגלה
            int borrowedBooksInCart = cart.Count(item => item.Type == "borrow");

            // ספרים מושאלים בפועל
            int currentBorrowedBooksCount = db.UserLibrary.Count(ul => ul.User_ID == userId && ul.IsBorrowed);

            // השאלה של אותו ספר 3 פעמים
            int bookBorrowedCount = db.UserLibrary.Count(ul => ul.Book_ID == id && ul.User_ID == userId && ul.IsBorrowed);

            if (borrowedBooksInCart >= 3)
            {
                return Json(new { success = false, message = "You cannot add more than 3 borrowed books to your cart." }, JsonRequestBehavior.AllowGet);
            }

            if (currentBorrowedBooksCount + borrowedBooksInCart >= 3)
            {
                return Json(new { success = false, message = "You cannot borrow more than 3 books at the same time." }, JsonRequestBehavior.AllowGet);
            }

            if (bookBorrowedCount >= 3)
            {
                return Json(new { success = false, message = $"The book '{book.Title}' has already been borrowed 3 times by you." }, JsonRequestBehavior.AllowGet);
            }

            // כל התנאים התקיימו
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }


        // פעולה להוספת ספר לעגלה
        public ActionResult AddToCart(string id, string type)
        {
            var book = db.Books.Find(id);
            if (book == null)
            {
                TempData["Message"] = "Book not found.";
                return RedirectToAction("Index");
            }

            // קבלת רשימת העגלה מה-Session
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // זיהוי משתמש נוכחי
            var userId = GetCurrentUserID();

            // בדיקה אם יש כבר 3 ספרים מושאלים בעגלה
            int borrowedBooksInCart = cart.Count(item => item.Type == "borrow");
            if (borrowedBooksInCart >= 3 && type == "borrow")
            {
                TempData["Message"] = "You cannot add more than 3 borrowed books to your cart.";
                return RedirectToAction("Index");
            }

            // בדיקה אם המשתמש כבר השאיל 3 ספרים
            int currentBorrowedBooksCount = db.UserLibrary
                .Count(ul => ul.User_ID == userId && ul.IsBorrowed);

            if (currentBorrowedBooksCount + borrowedBooksInCart >= 3 && type == "borrow")
            {
                TempData["Message"] = "You cannot borrow more than 3 books at the same time.";
                return RedirectToAction("Index");
            }

            // בדיקה אם הספר הזה כבר הושאל 3 פעמים
            int bookBorrowedCount = db.UserLibrary
                .Count(ul => ul.Book_ID == id && ul.User_ID == userId && ul.IsBorrowed);

            if (bookBorrowedCount >= 3 && type == "borrow")
            {
                TempData["Message"] = $"The book '{book.Title}' has already been borrowed 3 times by you.";
                return RedirectToAction("Index");
            }

            // הוספת הספר לעגלה
            cart.Add(new CartItem
            {
                Book = book,
                Type = type
            });

            // שמירה חזרה ל-Session
            Session["Cart"] = cart;

            // עדכון ספירת הפריטים בעגלה
            Session["CartCount"] = cart.Count;

            TempData["Message"] = $"{book.Title} has been added to your cart as a {(type == "borrow" ? "Borrowed" : "Purchased")} item.";
            return RedirectToAction("Index");
        }

        public ActionResult Cart()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View(cart);
        }

        //public ActionResult Cart()
        //{
        //    // בדיקה אם Session["Cart"] קיים, ואם לא, מחזיר רשימה ריקה
        //    var cart = Session["Cart"] as List<DigitalLibrary_NBA_IT.Models.Books> ?? new List<DigitalLibrary_NBA_IT.Models.Books>();
        //    return View(cart);
        //}

        // פעולה להשאלת ספר (כבר קיימת אך לא נדרשת כעת לשימוש)
        public ActionResult Borrow(string id)
        {
            var book = db.Books.Find(id);

            if (book != null)
            {
                int availableCopies = int.Parse(book.CopiesAvailable);

                if (availableCopies > 0)
                {
                    // הפחתת מספר העותקים הזמינים
                    book.CopiesAvailable = (availableCopies - 1).ToString();
                    db.SaveChanges();

                    TempData["Message"] = "Book borrowed successfully! Return it within 30 days.";
                }
                else
                {
                    TempData["Message"] = "No copies available. You've been added to the waitlist.";
                    AddToWaitlist(id);
                }
            }
            else
            {
                TempData["Message"] = "Book not found.";
            }

            return RedirectToAction("Index");
        }

        // הוספת משתמש לרשימת ההמתנה
        private void AddToWaitlist(string bookId)
        {
            var waitlist = new WAITLIST
            {
                Book_ID = bookId,
                DateAdded = DateTime.Now
            };

            db.WAITLIST.Add(waitlist);
            db.SaveChanges();
        }

        // זיהוי משתמש נוכחי
        private string GetCurrentUserId()
        {
            return HttpContext.User.Identity.Name ?? "guest";
        }

        // פעולה לניהול רשימת המתנה
        public ActionResult NotifyWaitlist(string bookId)
        {
            var waitlistEntries = db.WAITLIST
                                    .Where(w => w.Book_ID == bookId)
                                    .OrderBy(w => w.DateAdded)
                                    .ToList();

            if (waitlistEntries.Any())
            {
                var firstInLine = waitlistEntries.First();
                db.WAITLIST.Remove(firstInLine);
                db.SaveChanges();

                TempData["Message"] = $"The book is now available for {firstInLine.User_ID}.";
            }

            return RedirectToAction("Index");
        }
    }
}
