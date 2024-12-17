using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class UserProfileController : Controller
    {
        private Digital_library_DBEntities db = new Digital_library_DBEntities();

        // מתודה לבדיקת תקינות סיסמה
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            // לפחות 6 תווים, לפחות אות אחת, לפחות ספרה אחת ולפחות תו מיוחד אחד
            var regex = new Regex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$");
            return regex.IsMatch(password);
        }

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
        // POST: UserProfile/ChangePassword - שינוי סיסמה מתוך פרופיל המשתמש
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string newPassword)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            // בדיקת תקינות הסיסמה
            if (!IsValidPassword(newPassword))
            {
                TempData["Message"] = "Password change failed. Your new password must meet the following criteria: " +
                                      "at least 6 characters, include one letter, one number, and one special character.";
                TempData["MessageType"] = "error";
                return RedirectToAction("Profile");
            }

            int userId = (int)Session["UserID"];
            var user = db.USERS.Find(userId);

            if (user != null && !string.IsNullOrEmpty(newPassword))
            {
                user.password = newPassword;
                db.SaveChanges();

                TempData["Message"] = "Password updated successfully.";
                TempData["MessageType"] = "success";
                return RedirectToAction("Profile");
            }

            TempData["Message"] = "Password change failed. Please try again.";
            TempData["MessageType"] = "error";
            return RedirectToAction("Profile");
        }

        // POST: UserProfile/DeleteAccount - מחיקת משתמש
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAccount()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            int userId = (int)Session["UserID"];
            var user = db.USERS.Find(userId);

            if (user != null)
            {
                db.USERS.Remove(user); // מחיקת המשתמש ממסד הנתונים
                db.SaveChanges();

                Session.Clear(); // נקה את הסשן
                TempData["Message"] = "Your account has been deleted successfully.";
                TempData["MessageType"] = "success";
                return RedirectToAction("Register", "User"); // הפניה לעמוד ההרשמה
            }

            TempData["Message"] = "Failed to delete account. Please try again.";
            TempData["MessageType"] = "error";
            return RedirectToAction("Profile");
        }

    }
}
