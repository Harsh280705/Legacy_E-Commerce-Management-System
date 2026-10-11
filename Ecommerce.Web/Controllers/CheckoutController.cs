using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Web.Infrastructure;

namespace Ecommerce.Web.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICheckoutService _checkout;
        private readonly ICartService _carts;
        public CheckoutController(ICheckoutService checkout, ICartService carts) { _checkout = checkout; _carts = carts; }
        private string CartId { get { return CartSession.GetCartId(Session); } }

        [HttpGet]
        public ActionResult Index()
        {
            var summary = _checkout.BuildSummary(CartId);
            if (summary.Cart.Lines.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }
            // Prefill from logged-in user.
            if (User.Identity.IsAuthenticated)
                summary.Form.Email = User.Identity.Name;
            Session["LE_CheckoutKey"] = CartSession.NewIdempotencyKey(Session);
            ViewBag.IdempotencyKey = Session["LE_CheckoutKey"];
            return View(summary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Place(CheckoutFormViewModel form, string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                idempotencyKey = CartSession.PeekIdempotencyKey(Session);
            if (!ModelState.IsValid)
            {
                var summary = _checkout.BuildSummary(CartId);
                summary.Form = form;
                ViewBag.IdempotencyKey = idempotencyKey ?? CartSession.NewIdempotencyKey(Session);
                return View("Index", summary);
            }
            // Valid input: hold it in session and show an explicit review step.
            // No order is created here or by viewing Review.
            Session["LE_CheckoutKey"] = idempotencyKey ?? CartSession.NewIdempotencyKey(Session);
            Session["LE_PendingForm"] = form;
            return RedirectToAction("Review");
        }

        // Step 4: explicit order review with server-calculated totals.
        // GET only renders; it never creates an order.
        [HttpGet]
        public ActionResult Review()
        {
            var form = Session["LE_PendingForm"] as CheckoutFormViewModel;
            if (form == null) return RedirectToAction("Index");
            var summary = _checkout.BuildSummary(CartId);
            if (summary.Cart.Lines.Count == 0)
            {
                Session.Remove("LE_PendingForm");
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }
            summary.Form = form;
            ViewBag.IdempotencyKey = CartSession.PeekIdempotencyKey(Session);
            return View(summary);
        }

        // Step 5: final placement. Same idempotency key => same order, never a duplicate.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm()
        {
            var form = Session["LE_PendingForm"] as CheckoutFormViewModel;
            if (form == null) return RedirectToAction("Index");
            string idempotencyKey = CartSession.PeekIdempotencyKey(Session);
            string email = User.Identity.IsAuthenticated ? User.Identity.Name : form.Email;
            var result = _checkout.PlaceOrder(CartId, form, email, idempotencyKey);
            Session.Remove("LE_PendingForm");
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction("Index");
            }
            // Rotate key so refresh/back cannot re-place the order.
            Session["LE_CheckoutKey"] = CartSession.NewIdempotencyKey(Session);
            return RedirectToAction("Success", new { orderNumber = result.Data.OrderNumber });
        }

        [HttpGet]
        public ActionResult Success(string orderNumber)
        {
            var order = _checkout.GetOrder(orderNumber);
            if (order == null) return HttpNotFound("Order not found.");
            return View(order);
        }
    }
}
