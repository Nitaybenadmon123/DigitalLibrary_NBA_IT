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
            var user = new USERS(); // יצירת אובייקט USERS ריק
            return View(user);
        }

        // POST: Register - קבלת פרטי משתמש חדש ושמירה במסד הנתונים
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(USERS user)
        {
            if (!IsValidPassword(user.password))
            {
                ModelState.AddModelError("password", "Password must be at least 6 characters long, contain at least one letter, one number, and one special character.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                user.registration_date = DateTime.Now; // הוספת תאריך הרשמה
                user.isAdmin = false; // כברירת מחדל המשתמש אינו מנהל
                db.USERS.Add(user);
                db.SaveChanges();

                return RedirectToAction("Login"); // הפניה למסך כניסה
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
                Session["UserID"] = user.user_id;
                Session["UserName"] = user.name;
                Session["IsAdmin"] = user.isAdmin;
                return RedirectToAction("Index", "Home"); // העברת המשתמש לדף הבית
            }

            ViewBag.Message = "Invalid email or password";
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
