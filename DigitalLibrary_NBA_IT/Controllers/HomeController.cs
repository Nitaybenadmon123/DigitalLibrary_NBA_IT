using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DigitalLibrary_NBA_IT.Models;

namespace DigitalLibrary_NBA_IT.Controllers
{
    public class HomeController : Controller
    {
        public JsonResult GetBookDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Book ID is missing." }, JsonRequestBehavior.AllowGet);
            }

            var book = db.Books.Find(id);
            if (book == null)
            {
                return Json(new { success = false, message = "Book not found." }, JsonRequestBehavior.AllowGet);
            }

            // מחברים
            var authors = db.Authors
                .Where(a => a.Book_ID == book.Book_ID)
                .Select(a => new { a.Name, a.Bio })
                .ToList();

            // ז'אנרים
            var genres = db.Genres
                .Where(g => g.Book_ID == book.Book_ID)
                .Select(g => g.Name)
                .ToList();

            // רשימת המתנה
            var waitlist = db.WAITLIST
                .Where(w => w.Book_ID == book.Book_ID)
                .Select(w => new { w.User_ID, w.DateAdded })
                .ToList();

            // חוות דעת (אם אין, מחזיר רשימה ריקה)
            var reviews = db.Reviews
                .Where(r => r.Book_ID == book.Book_ID)
                .Select(r => new
                {
                    r.User_ID,
                    r.Rating,
                    r.Feedback,
                    r.ReviewDate
                })
                .ToList();

            var viewModel = new
            {
                Book = new
                {
                    book.Book_ID,
                    book.Title,
                    book.Publish,
                    book.Price,
                    book.CopiesAvailable,
                    book.ImageUrl
                },
                Authors = authors,
                Genres = genres,
                Waitlist = waitlist,
                WaitlistCount = waitlist.Count,
                Reviews = reviews // רשימה ריקה אם אין ביקורות
            };

            return Json(new { success = true, data = viewModel }, JsonRequestBehavior.AllowGet);
        }



        // חיבור למסד הנתונים
        private Digital_library_DBEntities db = new Digital_library_DBEntities();
        public JsonResult GetGenres()
        {
            var genres = db.Genres
                .GroupBy(g => new { g.Name })
                .Select(g => new
                {
                    Name = g.Key.Name
                }).ToList();

            return Json(genres, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetBooksByGenre(string genreName)
        {
            var books = db.Genres
                .Where(g => g.Name == genreName) // מסנן לפי שם הז'אנר
                .Select(g => db.Books.FirstOrDefault(b => b.Book_ID == g.Book_ID)) // מבטיח התאמה לספרים
                .Distinct()
                .ToList();

            return Json(books, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAuthors()
        {
            var authors = db.Authors
                .GroupBy(a => new { a.Name, a.Bio })
                .Select(g => new
                {
                    Name = g.Key.Name,
                    Bio = g.Key.Bio
                }).ToList();

            return Json(authors, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetBooksByAuthor(string authorName)
        {
            var books = db.Authors
                .Where(a => a.Name == authorName) // מסנן לפי שם המחבר
                .Select(a => a.Books) // מבצע נוויגציה לטבלת הספרים
                .SelectMany(book => db.Books.Where(b => b.Book_ID == book.Book_ID)) // מבטיח התאמה לספרים של המחבר בלבד
                .Distinct()
                .ToList();

            return Json(books, JsonRequestBehavior.AllowGet);
        }




        // פעולה ראשית להצגת הספרים וחיפוש
        public ActionResult Index(string query = "", string authorName = "", string sortOption = "", string genreName = "", decimal minPrice = 5, decimal maxPrice = 50)
        {
            var booksQuery = db.Books.AsQueryable();

            // חיפוש לפי שם הספר או המו"ל
            if (!string.IsNullOrEmpty(query))
            {
                booksQuery = booksQuery.Where(b => b.Title.Contains(query) || b.Publish.Contains(query));
            }

            // סינון לפי שם המחבר
            if (!string.IsNullOrEmpty(authorName))
            {
                booksQuery = booksQuery.Where(b => db.Authors.Any(a => a.Name == authorName && a.Book_ID == b.Book_ID));
            }
            // סינון לפי ז'אנר
            if (!string.IsNullOrEmpty(genreName))
            {
                booksQuery = booksQuery.Where(b => db.Genres.Any(g => g.Name == genreName && g.Book_ID == b.Book_ID));
            }
            // שליפת כל הנתונים לזיכרון לסינון לפי מחיר
            var booksList = booksQuery.ToList();

            booksList = booksList.Where(b =>
            {
                decimal price;
                if (decimal.TryParse(b.Price, out price))
                {
                    return price >= minPrice && price <= maxPrice;
                }
                return false;
            }).ToList();

            // המרה חזרה ל-IQueryable להמשך עיבוד
            booksQuery = booksList.AsQueryable();

            // הבאת הנתונים למסגרת הזיכרון
            var books = booksQuery.ToList();

            // מיון הספרים לפי האפשרות שנבחרה
            switch (sortOption)
            {
                case "priceAsc":
                    books = books.OrderBy(b => ParseDecimal(b.Price)).ToList();
                    break;
                case "priceDesc":
                    books = books.OrderByDescending(b => ParseDecimal(b.Price)).ToList();
                    break;
                case "popular":
                    books = books.OrderByDescending(b => ParseDecimal(b.Price) > 20).ThenBy(b => b.Title).ToList();
                    break;
                case "year":
                    books = books.OrderBy(b => b.Publish).ToList(); // הנחה ש-YearPublished הוא שדה במודל
                    break;
                case "discount":
                    books = books.Where(b => ParseDecimal(b.Price) < 10).ToList();
                    break;
            }

            // שמירה בתצוגה
            ViewBag.Query = query;
            ViewBag.GenreName = genreName;
            ViewBag.AuthorName = authorName;
            ViewBag.SortOption = sortOption;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;


            return View(books);
        }

        private decimal ParseDecimal(string priceString)
        {
            if (decimal.TryParse(priceString?.Trim(), out var price))
            {
                return price;
            }
            return decimal.MaxValue; // ערך גבוה אם המחיר לא תקין
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
        public ActionResult RemoveFromCart(string bookId)
        {
            // קבלת רשימת העגלה מה-Session
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // מציאת הספר לפי מזהה
            var itemToRemove = cart.FirstOrDefault(c => c.Book.Book_ID == bookId); // התאמת שם השדה
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove); // הסרת הספר מהעגלה
            }

            // שמירה חזרה ל-Session
            Session["Cart"] = cart;

            // עדכון ספירת הפריטים בעגלה
            Session["CartCount"] = cart.Count;

            // חזרה לעמוד העגלה
            return RedirectToAction("Cart");
        }
        private int GetCurrentUserID()
        {
            // לדוגמה: אם מזהה המשתמש נשמר ב-Session
            return int.Parse(Session["UserId"].ToString());
        }
        [HttpGet]
        public JsonResult CheckBorrowConditions(string id)
        {
            var userId = GetCurrentUserID();
            var book = db.Books.Find(id);

            if (book == null)
            {
                return Json(new { success = false, message = "Book not found." }, JsonRequestBehavior.AllowGet);
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // ספרים מושאלים בעגלה
            int borrowedBooksInCart = cart.Count(item => item.Type == "borrow");

            // ספרים מושאלים בפועל
            int currentBorrowedBooksCount = db.UserLibrary.Count(ul => ul.User_ID == userId && ul.IsBorrowed);

            // השאלה של אותו ספר 3 פעמים
            int bookBorrowedCount = db.UserLibrary.Count(ul => ul.Book_ID == id && ul.IsBorrowed);

            if (borrowedBooksInCart >= 3)
            {
                return Json(new { success = false, message = "You cannot add more than 3 borrowed books to your cart." }, JsonRequestBehavior.AllowGet);
            }

            if (currentBorrowedBooksCount + borrowedBooksInCart >= 3)
            {
                return Json(new { success = false, message = "You cannot borrow more than 3 books at the same time." }, JsonRequestBehavior.AllowGet);
            }

            if (bookBorrowedCount >= 3)
            {
                var waitlistEntry = new WAITLIST
                {
                    Book_ID = id, // מזהה הספר
                    User_ID = userId, // מזהה המשתמש
                    DateAdded = DateTime.Now // תאריך נוכחי
                };

                db.WAITLIST.Add(waitlistEntry);
                db.SaveChanges(); // שמירת השינויים למסד הנתונים

                return Json(new { success = false, message = $"The book '{book.Title}' has already been borrowed 3 times. You have been added to the waitlist." }, JsonRequestBehavior.AllowGet);
            }

            // כל התנאים התקיימו
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }


        // פעולה להוספת ספר לעגלה
        public ActionResult AddToCart(string id, string type)
        {

            if (Session["UserID"] == null)
            {
                TempData["Message"] = "You must be logged in to add items to your cart.please log in or register ";
                return RedirectToAction("Login", "User");
            }

            var book = db.Books.Find(id);
            if (book == null)
            {
                TempData["Message"] = "Book not found.";
                return RedirectToAction("Index");
            }

            // קבלת רשימת העגלה מה-Session
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();

            // זיהוי משתמש נוכחי
            var userId = GetCurrentUserID();

            // בדיקה אם יש כבר 3 ספרים מושאלים בעגלה
            int borrowedBooksInCart = cart.Count(item => item.Type == "borrow");
            if (borrowedBooksInCart >= 3 && type == "borrow")
            {
                TempData["Message"] = "You cannot add more than 3 borrowed books to your cart.";
                return RedirectToAction("Index");
            }

            // בדיקה אם המשתמש כבר השאיל 3 ספרים
            int currentBorrowedBooksCount = db.UserLibrary
                .Count(ul => ul.User_ID == userId && ul.IsBorrowed);

            if (currentBorrowedBooksCount + borrowedBooksInCart >= 3 && type == "borrow")
            {
                TempData["Message"] = "You cannot borrow more than 3 books at the same time.";
                return RedirectToAction("Index");
            }

            // בדיקה אם הספר הזה כבר הושאל 3 פעמים
            int bookBorrowedCount = db.UserLibrary
                .Count(ul => ul.Book_ID == id && ul.User_ID == userId && ul.IsBorrowed);

            if (bookBorrowedCount >= 3 && type == "borrow")
            {
                TempData["Message"] = $"The book '{book.Title}' has already been borrowed 3 times by you.";
                return RedirectToAction("Index");
            }

            // הוספת הספר לעגלה
            cart.Add(new CartItem
            {
                Book = book,
                Type = type
            });

            // שמירה חזרה ל-Session
            Session["Cart"] = cart;

            // עדכון ספירת הפריטים בעגלה
            Session["CartCount"] = cart.Count;

            TempData["Message"] = $"{book.Title} has been added to your cart as a {(type == "borrow" ? "Borrowed" : "Purchased")} item.";
            return RedirectToAction("Index");
        }

        public ActionResult Cart()
        {
            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View(cart);
        }

      

        // פעולה להשאלת ספר (כבר קיימת אך לא נדרשת כעת לשימוש)
        //public ActionResult Borrow(string id)
        //{
        //    var book = db.Books.Find(id);

        //    if (book != null)
        //    {
        //        int availableCopies = int.Parse(book.CopiesAvailable);

        //        if (availableCopies > 0)
        //        {
        //            // הפחתת מספר העותקים הזמינים
        //            book.CopiesAvailable = (availableCopies - 1).ToString();
        //            db.SaveChanges();

        //            TempData["Message"] = "Book borrowed successfully! Return it within 30 days.";
        //        }
        //        else
        //        {
        //            TempData["Message"] = "No copies available. You've been added to the waitlist.";
        //            AddToWaitlist(id);
        //        }
        //    }
        //    else
        //    {
        //        TempData["Message"] = "Book not found.";
        //    }

        //    return RedirectToAction("Index");
        //}

        //// הוספת משתמש לרשימת ההמתנה
        //private void AddToWaitlist(string bookId)
        //{
        //    var waitlist = new WAITLIST
        //    {
        //        Book_ID = bookId,
        //        DateAdded = DateTime.Now
        //    };

        //    db.WAITLIST.Add(waitlist);
        //    db.SaveChanges();
        //}

        // זיהוי משתמש נוכחי
        private string GetCurrentUserId()
        {
            return HttpContext.User.Identity.Name ?? "guest";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeCartItemType(string bookId, string newType)
        {
            if (string.IsNullOrEmpty(bookId) || string.IsNullOrEmpty(newType))
            {
                TempData["Message"] = "Invalid request.";
                return RedirectToAction("Cart");
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            var userId = GetCurrentUserID();

            // מציאת הפריט בעגלה
            var item = cart.FirstOrDefault(c => c.Book.Book_ID == bookId);
            if (item != null)
            {
                if (newType.ToLower() == "borrow")
                {
                    // בדיקת התנאים להשאלה
                    int borrowedBooksInCart = cart.Count(c => c.Type == "borrow" && c.Book.Book_ID != bookId);
                    int currentBorrowedBooksCount = db.UserLibrary.Count(ul => ul.User_ID == userId && ul.IsBorrowed);
                    int totalBorrowedBooks = borrowedBooksInCart + currentBorrowedBooksCount;

                    // בדיקה אם המשתמש כבר הגיע למגבלת ההשאלה
                    if (totalBorrowedBooks >= 3)
                    {
                        TempData["Message"] = "You cannot borrow more than 3 books at the same time.";
                        return RedirectToAction("Cart");
                    }

                    // בדיקה אם הספר הזה כבר הושאל 3 פעמים
                    int bookBorrowedCount = db.UserLibrary.Count(ul => ul.Book_ID == bookId && ul.IsBorrowed);
                    if (bookBorrowedCount >= 3)
                    {
                        // הוספת המשתמש לרשימת ההמתנה
                        var waitlistEntry = new WAITLIST
                        {
                            Book_ID = bookId,
                            User_ID = userId,
                            DateAdded = DateTime.Now
                        };
                        db.WAITLIST.Add(waitlistEntry);
                        db.SaveChanges(); // שמירת השינויים במסד הנתונים

                        TempData["Message"] = "This book is already borrowed by 3 users. You have been added to the waitlist.";
                        return RedirectToAction("Cart");
                    }

                    // עדכון הסוג להשאלה
                    item.Type = "borrow";
                }
                else
                {
                    // עדכון הסוג לקנייה
                    item.Type = "buy";
                }
            }

            // שמירת העדכונים בעגלה
            Session["Cart"] = cart;

            TempData["Message"] = "Cart item updated successfully.";
            return RedirectToAction("Cart");
        }

        public ActionResult Welcome()
        {
            var books = db.Books.ToList(); // שליפת כל הספרים
            return View(books);
        }
        [HttpGet]
        public JsonResult GetSiteFeedback()
        {
            var feedbacks = db.SiteFeedback
                .Select(f => new
                {
                    Stars = f.Stars,
                    Feedback = f.Feedback,
                    CreatedDate = f.CreatedDate
                })
                .OrderByDescending(f => f.CreatedDate)
                .ToList();

            return Json(feedbacks, JsonRequestBehavior.AllowGet);
        }



    }
}
