using System.Web.Mvc;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICatalogService _catalog;
        private readonly IDatabaseHealthCheck _health;
        public HomeController(ICatalogService catalog, IDatabaseHealthCheck health) { _catalog = catalog; _health = health; }

        public ActionResult Index()
        {
            ViewBag.Categories = _catalog.GetCategoryTree();
            ViewBag.Featured = _catalog.GetFeatured(8);
            ViewBag.NewArrivals = _catalog.GetNewArrivals(8);
            ViewBag.Discounted = _catalog.GetDiscounted(4);
            return View();
        }

        // Plain-text health probe used by Docker/Nginx (checks DB connectivity too).
        // Same URL, format and status codes as before; no credentials or DB errors leak.
        public ActionResult Health()
        {
            if (_health.IsHealthy())
                return Content("OK", "text/plain");
            Response.StatusCode = 503;
            return Content("DB_UNAVAILABLE", "text/plain");
        }

        public ActionResult About() { return View(); }
        public ActionResult Contact() { return View(); }
        public ActionResult Shipping() { return View(); }
        public ActionResult Returns() { return View(); }
        public ActionResult Privacy() { return View(); }
        public ActionResult Error() { return View(); }
    }
}
