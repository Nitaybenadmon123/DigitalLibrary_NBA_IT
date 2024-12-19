using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class UserController : Controller
    {
        private Digital_library_DBEntities db = new Digital_library_DBEntities();

        // בדיקה אם סיסמה עומדת בקריטריונים
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            // לפחות 6 תווים, לפחות אות אחת, לפחות ספרה אחת ולפחות תו מיוחד אחד
            var regex = new Regex(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$");
            return regex.IsMatch(password);
        }

        // GET: Register - הצגת טופס הרשמה
        public ActionResult Register()
        {
            return View(new USERS());
        }

        // POST: Register - קבלת פרטי משתמש חדש ושמירה במסד הנתונים
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(USERS user)
        {
            // בדיקה אם הסיסמה עומדת בקריטריונים
            if (!IsValidPassword(user.password))
            {
                TempData["Message"] = "Password must be at least 6 characters, include one number, and one special character.";
                return RedirectToAction("Register");
            }

            if (db.USERS.Any(u => u.email == user.email))
            {
                TempData["Message"] = "The email address is already in use. Please try another.";
                return RedirectToAction("Register");
            }

            if (db.USERS.Any(u => u.name == user.name))
            {
                TempData["Message"] = "The user name is already in use. Please try another.";
                return RedirectToAction("Register");
            }

            if (ModelState.IsValid)
            {
                user.registration_date = DateTime.Now;
                user.isAdmin = false; // משתמש רגיל כברירת מחדל
                db.USERS.Add(user);
                db.SaveChanges();
                TempData["Message"] = "Registration successful! Please log in.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // GET: Login - הצגת טופס התחברות
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login - בדיקת פרטי התחברות
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            var user = db.USERS.FirstOrDefault(u => u.email == email && u.password == password);

            if (user != null)
            {
                if (user.isAdmin)
                {
                    Session["UserID"] = user.user_id;
                    Session["UserName"] = user.name;
                    Session["IsAdmin"] = true;
                    return RedirectToAction("AdminDashboard");
                }
                else
                {
                    Session["UserID"] = user.user_id;
                    Session["UserName"] = user.name;
                    Session["IsAdmin"] = false;
                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["Message"] = "Invalid email or password.";
            return RedirectToAction("Login");
        }

        // GET: AdminDashboard - עמוד מנהל
        public ActionResult AdminDashboard()
        {
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                TempData["Message"] = "Access denied. Admins only.";
                return RedirectToAction("Login");
            }

            return View();
        }

        // Logout - יציאה מהמערכת
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
