using System;
using System.Linq;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class LibraryController : Controller
    {
        private readonly Digital_library_DBEntities db = new Digital_library_DBEntities();

        // פעולה להצגת הספרייה האישית של המשתמש
        [HttpGet]
        public ActionResult PersonalLibrary()
        {
            if (Session["UserID"] == null)
            {
                TempData["Error"] = "You must be logged in to access your library.";
                return RedirectToAction("Index", "Home");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // שליפת ספרים מהספרייה האישית
            var userLibrary = db.UserLibrary
                .Where(ul => ul.User_ID == userId)
                .ToList();

            // סינון ספרים מושאלים שפג תוקפם
            userLibrary = userLibrary
                .Where(book => !(book.IsBorrowed && book.ExpiryDate.HasValue && DateTime.Now > book.ExpiryDate))
                .ToList();

            return View(userLibrary);
        }



        // פעולה למחיקת ספר שנקנה
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveBook(string bookId)
        {
            if (Session["UserID"] == null)
            {
                TempData["Error"] = "You must be logged in to remove a book.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(bookId))
            {
                TempData["Error"] = "Book ID is missing.";
                return RedirectToAction("PersonalLibrary");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // מציאת הספר להסרה
            var bookToRemove = db.UserLibrary
                .FirstOrDefault(ul => ul.Book_ID.Trim() == bookId.Trim() && ul.User_ID == userId && !ul.IsBorrowed);

            if (bookToRemove == null)
            {
                // הספר אינו ספר שנרכש
                TempData["Error"] = "The book cannot be removed or does not exist in your purchased library.";
                return RedirectToAction("PersonalLibrary");
            }

            try
            {
                // הסרת הספר
                db.UserLibrary.Remove(bookToRemove);
                db.SaveChanges();
                TempData["Message"] = "Book successfully removed from your library.";
            }
            catch (Exception ex)
            {
                // טיפול בשגיאה בזמן מחיקה
                TempData["Error"] = $"An error occurred while trying to remove the book: {ex.Message}";
            }

            return RedirectToAction("PersonalLibrary");
        }


        [HttpGet]
        public ActionResult AddReview(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound("Book ID is missing.");
            }

            var book = db.Books.FirstOrDefault(b => b.Book_ID.Trim() == id.Trim());
            if (book == null)
            {
                return HttpNotFound("Book not found.");
            }

            ViewBag.BookTitle = book.Title;
            ViewBag.BookId = book.Book_ID;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(string bookId, string reviewText, int rating)
        {
            if (string.IsNullOrWhiteSpace(bookId))
            {
                TempData["Error"] = "Book ID is missing.";
                return RedirectToAction("PersonalLibrary");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var bookExists = db.UserLibrary.Any(ul => ul.Book_ID.Trim() == bookId.Trim() && ul.User_ID == userId);

            if (!bookExists)
            {
                TempData["Error"] = "You cannot review a book you haven't purchased or borrowed.";
                return RedirectToAction("PersonalLibrary");
            }

            var review = new Reviews
            {
                Book_ID = bookId,
                User_ID = userId,
                Feedback = reviewText,
                Rating = rating,
                ReviewDate = DateTime.Now
            };

            db.Reviews.Add(review);
            db.SaveChanges();

            TempData["Message"] = "Your review has been added successfully.";
            return RedirectToAction("PersonalLibrary");
        }

        [HttpGet]
        public ActionResult DownloadBook(string bookId, string format)
        {
            if (Session["UserID"] == null)
            {
                TempData["Error"] = "You must be logged in to download a book.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(bookId) || string.IsNullOrWhiteSpace(format))
            {
                TempData["Error"] = "Book ID or format is missing.";
                return RedirectToAction("PersonalLibrary");
            }

            // בדיקת ספרים במאגר
            var book = db.Books.FirstOrDefault(b => b.Book_ID.Trim() == bookId.Trim());
            if (book == null)
            {
                TempData["Error"] = "Book not found.";
                return RedirectToAction("PersonalLibrary");
            }

            // בדיקת פורמט תקין
            var allowedFormats = new[] { "mobi", "b2f", "epub", "pdf" };
            if (!allowedFormats.Contains(format.ToLower()))
            {
                TempData["Error"] = "Invalid format selected.";
                return RedirectToAction("PersonalLibrary");
            }

            // יצירת תוכן הקובץ הריק
            var fileName = $"{book.Title}.{format}";
            var fileContent = $"This is a placeholder file for the book '{book.Title}' in {format.ToUpper()} format.";

            // הורדת הקובץ
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(fileContent);
            return File(fileBytes, "application/octet-stream", fileName);
        }



    }
}

