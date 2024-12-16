using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["Digital_library_DBEntities"].ConnectionString;

        // הוספת DbContext לקריאה ממסד הנתונים
        private Digital_library_DBEntities db = new Digital_library_DBEntities();

        public ActionResult Index()
        {
            // שאילתא לקריאה מטבלת BOOKS
            var books = db.Books.ToList();

            // העברת הנתונים ל-View
            return View(books);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
