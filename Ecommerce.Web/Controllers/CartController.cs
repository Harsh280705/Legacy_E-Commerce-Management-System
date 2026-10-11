using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Web.Infrastructure;

namespace Ecommerce.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _carts;
        public CartController(ICartService carts) { _carts = carts; }
        private string CartId { get { return CartSession.GetCartId(Session); } }

        [HttpGet]
        public ActionResult Index()
        {
            return View(_carts.GetCart(CartId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(int productId, int? variantId, int quantity = 1)
        {
            var r = _carts.AddToCart(CartId, productId, variantId, quantity);
            if (Request.IsAjaxRequest())
                return Json(new { success = r.Success, message = r.Message, count = _carts.GetCartCount(CartId) });
            TempData[r.Success ? "Notice" : "Error"] = r.Message;
            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : "/");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int cartItemId, int quantity)
        {
            var r = _carts.UpdateQuantity(CartId, cartItemId, quantity);
            if (Request.IsAjaxRequest())
            {
                ViewBag.CartMessage = r.Message;
                ViewBag.CartOk = r.Success;
                return PartialView("_CartLines", _carts.GetCart(CartId));
            }
            TempData[r.Success ? "Notice" : "Error"] = r.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int cartItemId)
        {
            var r = _carts.RemoveItem(CartId, cartItemId);
            if (Request.IsAjaxRequest())
            {
                ViewBag.CartMessage = r.Message;
                ViewBag.CartOk = r.Success;
                return PartialView("_CartLines", _carts.GetCart(CartId));
            }
            TempData[r.Success ? "Notice" : "Error"] = r.Message;
            return RedirectToAction("Index");
        }

        // Mini-cart partial + count (polled/refreshed by site.js).
        [HttpGet]
        public ActionResult Mini() { return PartialView("_MiniCart", _carts.GetCart(CartId)); }
        [HttpGet]
        public ActionResult Count() { return Json(new { count = _carts.GetCartCount(CartId) }, JsonRequestBehavior.AllowGet); }
    }
}
