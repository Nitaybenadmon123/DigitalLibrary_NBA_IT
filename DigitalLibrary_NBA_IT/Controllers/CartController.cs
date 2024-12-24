//using System.Collections.Generic;
//using System.Linq;
//using System.Web.Mvc;

//namespace DigitalLibrary_NBA_IT.Controllers
//{
//    public class CartController : Controller
//    {
//        // פעולה להצגת עגלת הקניות
//        public ActionResult Index()
//        {
//            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
//            return View(cart);
//        }

//        // פעולה להסרת פריט מהעגלה
//        public ActionResult RemoveFromCart(int bookId)
//        {
//            var cart = Session["Cart"] as List<CartItem>;
//            if (cart != null)
//            {
//                var item = cart.FirstOrDefault(c => c.BookID == bookId);
//                if (item != null)
//                {
//                    cart.Remove(item);
//                }

//                Session["Cart"] = cart;
//            }

//            return RedirectToAction("Index");
//        }
//    }
//}
