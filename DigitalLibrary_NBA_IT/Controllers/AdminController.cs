using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class AdminController : Controller
    {
        public ActionResult AdminDashboard()
        {
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                return RedirectToAction("Login", "User"); // הפניה אם המשתמש לא מנהל
            }

            return View();
        }
    }

}