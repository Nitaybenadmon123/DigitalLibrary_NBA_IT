using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;
using BCrypt.Net;
using System.Data.Entity;
using System.Security.Cryptography;
using System.Text;


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


                //הצפנה בשיטה החדשה של אנה
                user.password = string.Concat(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(user.password)).Select(b => b.ToString("x2")));

                // הצפנת הסיסמה לפני שמירה למסד
                //user.password = BCrypt.Net.BCrypt.HashPassword(user.password);
               
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
           
            // התחברות רגילה עם הצפנה
            var regularUser = db.USERS.FirstOrDefault(u => u.email.ToLower() == email.ToLower());

            if (regularUser != null)
            {
                string hashedInput = string.Concat(
                    SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password))
                    .Select(b => b.ToString("x2"))
                );

                if (regularUser.password == hashedInput)
                {
                    HandleExpiredLoansForAllUsers();

                    Session["UserID"] = regularUser.user_id;
                    Session["UserName"] = regularUser.name;
                    Session["IsAdmin"] = regularUser.isAdmin;

                    if (regularUser.isAdmin)
                    {
                        return RedirectToAction("AdminDashboard");
                    }
                    else
                    {
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


        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            var user = db.USERS.FirstOrDefault(u => u.email == email);
            if (user != null)
            {
                // צור קוד אקראי
                string resetCode = new Random().Next(100000, 999999).ToString();

                // שמירת הקוד ב-Session או TempData
                Session["ResetCode"] = resetCode; // שמור את הקוד
                Session["Email"] = email; // שמור את כתובת המייל


                // שליחת המייל עם הקוד
                var emailService = new EmailService();
                emailService.SendEmail(email, "Password Reset Code", $"Your reset code is: {resetCode}");

                TempData["Message"] = "A password reset code has been sent to your email.";
                return RedirectToAction("VerifyCode");
            }

            TempData["Error"] = "Email not found.";
            return View();
        }

        [HttpGet]
        public ActionResult VerifyCode()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyCode(string code)
        {
            string storedCode = Session["ResetCode"] as string;
            string email = Session["Email"] as string;

            if (storedCode == code)
            {
                Session["Email"] = email; // שמירה לשלב הבא
                return RedirectToAction("ResetPassword");
            }

            TempData["Error"] = "Invalid reset code. Please try again.";
            return View();
        }


        [HttpGet]
        public ActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult ResetPassword(string newPassword)
        {
            string email = Session["Email"] as string;

            if (!string.IsNullOrEmpty(email))
            {
                var user = db.USERS.FirstOrDefault(u => u.email == email);
                if (user != null)
                {
                    // בדיקה אם הסיסמה עומדת בקריטריונים
                    if (!IsValidPassword(newPassword))
                    {
                        TempData["Error"] = "Password must be at least 6 characters, include one number, and one special character.";
                        return RedirectToAction("ResetPassword");
                    }

                    // הצפנת הסיסמה ושמירה
                    user.password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    db.SaveChanges();

                    TempData["Message"] = "Your password has been reset successfully.";
                    Session.Remove("ResetCode");
                    return RedirectToAction("Login");
                }
            }

            TempData["Error"] = "Failed to reset password.";
            return View();
        }

        private void HandleExpiredLoansForAllUsers()
        {
            var emailService = new EmailService();

            // שליפת השאלות שפג תוקפן
            var expiredLoans = db.UserLibrary
                .Where(ul => ul.IsBorrowed && ul.ExpiryDate.HasValue && ul.ExpiryDate <= DateTime.Now)
                .ToList();

            foreach (var loan in expiredLoans)
            {
                var book = db.Books.FirstOrDefault(b => b.Book_ID == loan.Book_ID);
                if (book != null)
                {
                    int activeBorrowCount = db.UserLibrary
               .Where(ul => ul.Book_ID == loan.Book_ID && ul.IsBorrowed)
               .Count();

                    // העלאת המלאי של הספר
                    if (int.TryParse(book.CopiesAvailable, out int copies))
                    {
                        book.CopiesAvailable = (copies + 1).ToString();
                    }
                    if (activeBorrowCount == 3)
                    {
                        NotifyWaitlist(loan.Book_ID, emailService);
                    }
                }

                // הסרת הספר מהטבלה של המשתמש
                db.UserLibrary.Remove(loan);
            }

            // שליפת השאלות שחמישה ימים לפני תום תקופת ההשאלה ושלא נשלחה להן תזכורת
            var soonToExpireLoans = db.UserLibrary
                .Where(ul => ul.IsBorrowed && ul.ExpiryDate.HasValue &&
                             DbFunctions.DiffDays(DateTime.Now, ul.ExpiryDate.Value) == 5 &&
                             ul.ReminderSent == false)
                .ToList();

            foreach (var loan in soonToExpireLoans)
            {
                var user = db.USERS.FirstOrDefault(u => u.user_id == loan.User_ID);
                var book = db.Books.FirstOrDefault(b => b.Book_ID == loan.Book_ID);

                if (user != null && book != null)
                {
                    // שליחת מייל למשתמש
                    string subject = $"Reminder: Borrowed book '{book.Title}' is due in 5 days";
                    string body = $@"
                Dear {user.name},<br/><br/>
                This is a friendly reminder that the borrowed book '<b>{book.Title}</b>' will be due in 5 days.<br/>
                Please make sure to return it on time to avoid any inconvenience.<br/><br/>
                Thank you for using our digital library!<br/>
                <i>Digital Library Team</i>";

                    try
                    {
                        emailService.SendEmail(user.email, subject, body);

                        // עדכון ReminderSent ל-true
                        loan.ReminderSent = true;
                    }
                    catch (Exception ex)
                    {
                        // טיפול בשגיאות שליחת מייל (לא יגרום להפסקת התהליך)
                        Console.WriteLine($"Failed to send email to {user.email}: {ex.Message}");
                    }
                }
            }

            db.SaveChanges();
        }

        private void NotifyWaitlist(string bookId, EmailService emailService)
        {
            // שליפת שלושת הראשונים ברשימת ההמתנה
            var waitlistEntries = db.WAITLIST
                .Where(w => w.Book_ID == bookId)
                .OrderBy(w => w.DateAdded)
                .Take(3)
                .ToList();

            foreach (var entry in waitlistEntries)
            {
                var user = db.USERS.FirstOrDefault(u => u.user_id == entry.User_ID);
                if (user != null && !string.IsNullOrEmpty(user.email))
                {
                    // שליחת מייל למשתמש
                    string subject = "Book Now Available";
                    string body = $@"
                    Dear {user.name},<br/><br/>
                    The book you requested '<b>{db.Books.FirstOrDefault(b => b.Book_ID == bookId)?.Title}'</b> is now available.<br/>
                    Please borrow it as soon as possible.only the first one who borrow it will get the book ! harry up ! <br/><br/>
                    Thank you for using our digital library!<br/>
                    <i>Digital Library Team</i>";

                    try
                    {
                        emailService.SendEmail(user.email, subject, body);

                        // הסרת המשתמש מרשימת ההמתנה
                        db.WAITLIST.Remove(entry);
                    }
                    catch (Exception ex)
                    {
                        // טיפול בשגיאות שליחת מייל (לא יגרום להפסקת התהליך)
                        Console.WriteLine($"Failed to send email to {user.email}: {ex.Message}");
                    }
                }
            }

            db.SaveChanges();
        }

        [HttpGet]
        public ActionResult SiteFeedback()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SiteFeedback(int stars, string feedback)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                int userId = (int)Session["UserID"];
                var siteFeedback = new SiteFeedback
                {
                    User_ID = userId,
                    Stars = stars,
                    Feedback = feedback,
                    CreatedDate = DateTime.Now
                };

                db.SiteFeedback.Add(siteFeedback);
                db.SaveChanges();

                TempData["Message"] = "Thank you for your feedback!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }



    }

}
