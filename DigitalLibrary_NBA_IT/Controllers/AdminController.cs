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

    }
}