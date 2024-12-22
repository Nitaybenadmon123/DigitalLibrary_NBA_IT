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
        public ActionResult ManagePrices(string query)
        {
            var books = db.Books.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                books = books.Where(b => b.Title.Contains(query) || b.Publish.Contains(query));
                ViewBag.Query = query; // שימור החיפוש בשדה
            }

            return View(books.ToList());
        }


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

            return RedirectToAction("ManagePrices");
        }
    }

}

