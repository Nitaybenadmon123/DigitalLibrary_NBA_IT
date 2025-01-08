using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;
using BCrypt.Net;

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
                // הצפנת הסיסמה החדשה
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.password = hashedPassword;

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
                try
                {
                    // 1. מחיקת רשומות מטבלת WAITLIST
                    var waitlistItems = db.WAITLIST.Where(w => w.User_ID == userId).ToList();
                    db.WAITLIST.RemoveRange(waitlistItems);
                    db.SaveChanges();

                    // 2. מחיקת רשומות מטבלת UserLibrary
                    var userLibraryItems = db.UserLibrary.Where(ul => ul.User_ID == userId).ToList();
                    db.UserLibrary.RemoveRange(userLibraryItems);
                    db.SaveChanges();

                    // 3. מחיקת רשומות מטבלת Reviews
                    var reviews = db.Reviews.Where(r => r.User_ID == userId).ToList();
                    db.Reviews.RemoveRange(reviews);
                    db.SaveChanges();

                    // 4. מחיקת המשתמש עצמו
                    db.USERS.Remove(user);
                    db.SaveChanges();

                    // נקה את הסשן
                    Session.Clear();

                    TempData["Message"] = "Your account and related data have been deleted successfully.";
                    TempData["MessageType"] = "success";
                    return RedirectToAction("Register", "User");
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.InnerException?.Message ?? ex.Message;
                    TempData["Message"] = $"An error occurred while deleting your account: {errorMessage}";
                    TempData["MessageType"] = "error";
                    return RedirectToAction("Profile");
                }
            }

            TempData["Message"] = "Failed to delete account. Please try again.";
            TempData["MessageType"] = "error";
            return RedirectToAction("Profile");
        }

    }
}
