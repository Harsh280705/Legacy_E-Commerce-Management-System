using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;
using Ecommerce.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace Ecommerce.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly ICheckoutService _orders;
        private readonly IAccountService _accounts;
        public AccountController(ICheckoutService orders, IAccountService accounts) { _orders = orders; _accounts = accounts; }

        private ApplicationUserManager UserManager { get { return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); } }

        // NOTE (verified on Mono/XSP 2026-10-11): HttpContext.Current does not flow
        // across awaits here, so Identity is called synchronously. Async EF/Identity
        // work is bridged via Task.Run (pool thread, no ASP.NET sync context) to
        // avoid deadlocks; cookie sign-in runs on the request thread.
        private static T RunSync<T>(System.Func<Task<T>> fn)
        {
            return Task.Run(fn).GetAwaiter().GetResult();
        }
        private static void RunSync(System.Func<Task> fn)
        {
            Task.Run(fn).GetAwaiter().GetResult();
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);
            var manager = UserManager; // capture on request thread
            var user = RunSync(() => manager.FindByEmailAsync(model.Email));
            bool ok = false;
            if (user != null)
            {
                if (RunSync(() => manager.IsLockedOutAsync(user.Id)))
                {
                    ModelState.AddModelError("", "Account locked. Try again later.");
                    return View(model);
                }
                ok = RunSync(() => manager.CheckPasswordAsync(user, model.Password));
                if (ok)
                {
                    RunSync(() => manager.ResetAccessFailedCountAsync(user.Id));
                    var identity = RunSync(() => user.GenerateUserIdentityAsync(manager));
                    var auth = HttpContext.GetOwinContext().Authentication;
                    auth.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                    auth.SignIn(new AuthenticationProperties { IsPersistent = model.RememberMe }, identity);
                    _accounts.EnsureCustomerRecord(user.Id, user.Email, null, null);
                    return RedirectToLocal(returnUrl);
                }
                RunSync(() => manager.AccessFailedAsync(user.Id));
            }
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Register() { return View(new RegisterViewModel()); }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var manager = UserManager; // capture on request thread
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FirstName = model.FirstName, LastName = model.LastName };
            var result = RunSync(() => manager.CreateAsync(user, model.Password));
            if (result.Succeeded)
            {
                _accounts.EnsureCustomerRecord(user.Id, user.Email, model.FirstName, model.LastName);
                var identity = RunSync(() => user.GenerateUserIdentityAsync(manager));
                HttpContext.GetOwinContext().Authentication.SignIn(
                    new AuthenticationProperties { IsPersistent = false }, identity);
                return RedirectToAction("Profile");
            }
            foreach (var e in result.Errors) ModelState.AddModelError("", e);
            return View(model);
        }

        public new ActionResult Profile()
        {
            ViewBag.Orders = _orders.GetOrderHistory(User.Identity.Name);
            return View();
        }

        public ActionResult Orders()
        {
            return View(_orders.GetOrderHistory(User.Identity.Name));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Profile");
        }
    }
}
