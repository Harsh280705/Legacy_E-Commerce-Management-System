using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Core.ViewModels;

namespace Ecommerce.Web.Controllers
{
    /// <summary>
    /// Admin area. Every action requires the Identity "Admin" role (checked
    /// against AspNetUserRoles on each request — ordinary users get 403).
    /// Anonymous users are redirected to login by [Authorize].
    /// </summary>
    [Authorize]
    public class AdminController : Controller
    {
        private readonly IAdminService _admin;
        private readonly ICatalogService _catalog;
        public AdminController(IAdminService admin, ICatalogService catalog) { _admin = admin; _catalog = catalog; }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (User.Identity.IsAuthenticated && !_admin.IsAdmin(User.Identity.Name))
                filterContext.Result = new HttpStatusCodeResult(403, "Administrators only.");
        }

        [HttpGet]
        public ActionResult Index(int threshold = 10)
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            return View(_admin.GetDashboard(threshold));
        }

        // Product management: paged search + category filter + sort via existing catalog service.
        [HttpGet]
        public ActionResult Products(string q, int? categoryId, string sort, int page = 1)
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            ViewBag.Query = q;
            ViewBag.CategoryId = categoryId;
            ViewBag.Sort = sort ?? "name_asc";
            return View(_catalog.SearchProducts(q, categoryId, sort, page, 12));
        }

        [HttpGet]
        public ActionResult Product(int id)
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            var vm = _admin.GetProductForAdmin(id);
            if (vm == null) return HttpNotFound("Product not found.");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStock(AdminStockUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Stock must be between 0 and 100000.";
                return RedirectToAction("Product", new { id = model.ProductId });
            }
            var stock = _admin.UpdateStock(model.ProductId, model.StockQuantity);
            var active = _admin.SetActive(model.ProductId, model.IsActive);
            TempData[stock.Success && active.Success ? "Notice" : "Error"] =
                stock.Success && active.Success ? "Saved." : (stock.Success ? active.Message : stock.Message);
            return RedirectToAction("Product", new { id = model.ProductId });
        }
    }
}
