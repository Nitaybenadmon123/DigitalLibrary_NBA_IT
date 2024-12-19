using System;
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
                                           || b.Publish.Contains(query)
                                           ) // ודא ששדה Author קיים
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
        // פעולה להשאלת ספר
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
                //User_ID = GetCurrentUserId(), // פונקציה לזיהוי המשתמש
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
