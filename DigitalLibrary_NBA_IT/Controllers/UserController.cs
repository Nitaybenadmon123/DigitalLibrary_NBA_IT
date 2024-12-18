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
            // קבלת הערך של תיבת הסימון
            bool isAdmin = Request.Form["isAdmin"] == "true";

            if (!IsValidPassword(user.password))
            {
                ModelState.AddModelError("password", "Password must meet the criteria.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                user.registration_date = DateTime.Now;
                user.isAdmin = isAdmin; // הגדרת האם הוא מנהל או לא
                db.USERS.Add(user);
                db.SaveChanges();

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
        public ActionResult Login(string email, string password, bool isAdminLogin = false)
        {
            // הדפסה לבדיקת הערכים שהתקבלו
            Console.WriteLine($"isAdminLogin: {isAdminLogin}");
            Console.WriteLine($"Email: {email}, Password: {password}");

            // בדוק את המשתמש מהמסד
            var user = db.USERS.FirstOrDefault(u => u.email == email && u.password == password);

            if (user != null)
            {
                Console.WriteLine($"User found: {user.name}, isAdmin: {user.isAdmin}");

                if (isAdminLogin)
                {
                    // התחברות כמנהל
                    if (user.isAdmin)
                    {
                        Session["UserID"] = user.user_id;
                        Session["UserName"] = user.name;
                        Session["IsAdmin"] = true;
                        TempData["Message"] = "Welcome, Admin!";
                        return RedirectToAction("AdminDashboard");
                    }
                    else
                    {
                        TempData["Message"] = "You do not have admin privileges.";
                        return RedirectToAction("Login");
                    }
                }
                else
                {
                    // התחברות רגילה
                    if (user.isAdmin)
                    {
                        TempData["Message"] = "Admins must log in using the 'Login as Admin' option.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        Session["UserID"] = user.user_id;
                        Session["UserName"] = user.name;
                        Session["IsAdmin"] = false;
                        TempData["Message"] = "Welcome, User!";
                        return RedirectToAction("Index", "Home");
                    }
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
