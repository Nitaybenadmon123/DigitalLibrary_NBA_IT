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

        // פעולה ראשית להצגת הספרים וחיפוש
        public ActionResult Index(string query = "")
        {
            var books = string.IsNullOrEmpty(query)
                        ? db.Books.ToList()
                        : db.Books.Where(b => b.Title.Contains(query)
                                           || b.Publish.Contains(query))
                                  .ToList();

            ViewBag.Query = query; // שמירה על השאילתה בתיבת החיפוש
            return View(books);
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

            // הוספת הספר לעגלה עם סוג הפעולה (קנייה או השאלה)
            cart.Add(new CartItem
            {
                Book = book,
                Type = type
            });

            // שמירה חזרה ל-Session
            Session["Cart"] = cart;

            // עדכון ספירת הפריטים בעגלה
            Session["CartCount"] = cart.Count;

            // הודעת הצלחה
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
