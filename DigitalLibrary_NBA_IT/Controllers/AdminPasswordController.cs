using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BCrypt.Net;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class AdminPasswordController : Controller
    {
        // GET: AdminPassword
        public ActionResult GeneratePassword()
        {
            // סיסמה רגילה להצפנה
            string plainPassword = "ADMIN123@";

            // הצפנת הסיסמה
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            // הצגת התוצאה בדפדפן
            return Content($"The hashed password is: {hashedPassword}");
        }
    }
}