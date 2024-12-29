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
            if (Session["UserId"] == null)
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
            if (Session["UserId"] == null)
            {
                TempData["Error"] = "You must be logged in to remove a book.";
                return RedirectToAction("Index", "Home");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var bookToRemove = db.UserLibrary
                .FirstOrDefault(ul => ul.Book_ID == bookId && ul.User_ID == userId && !ul.IsBorrowed);

            if (bookToRemove != null)
            {
                db.UserLibrary.Remove(bookToRemove);
                db.SaveChanges();
                TempData["Message"] = "Book successfully removed from your library.";
            }
            else
            {
                TempData["Error"] = "Failed to remove the book.";
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


    }
}

