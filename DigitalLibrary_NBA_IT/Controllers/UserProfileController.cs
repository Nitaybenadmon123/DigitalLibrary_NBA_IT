using System;
using System.Linq;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class UserProfileController : Controller
    {
        private Digital_library_DBEntities db = new Digital_library_DBEntities();

        // GET: UserProfile/Profile - הצגת פרטי משתמש
        public ActionResult Profile()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            int userId = (int)Session["UserID"];
            var user = db.USERS.Find(userId);

            if (user != null)
            {
                return View(user); // מפנה ל-Profile.cshtml
            }

            return RedirectToAction("Login", "User");
        }

        // POST: UserProfile/ChangePassword - שינוי סיסמה מתוך פרופיל המשתמש
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string newPassword)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            int userId = (int)Session["UserID"];
            var user = db.USERS.Find(userId);

            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                user.password = newPassword;
                db.SaveChanges();

                ViewBag.Message = "Password updated successfully.";
                return RedirectToAction("Profile"); // מפנה לעמוד הפרופיל
            }

            ViewBag.Message = "Failed to update password. Please try again.";
            return RedirectToAction("Profile");
        }
    }
}
