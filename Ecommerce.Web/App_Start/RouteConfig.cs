using System.Web.Mvc;
using System.Web.Routing;

namespace Ecommerce.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute("Health", "health", new { controller = "Home", action = "Health" });
            routes.MapRoute("Product", "p/{slug}", new { controller = "Catalog", action = "Product" });
            routes.MapRoute("Category", "c/{slug}", new { controller = "Catalog", action = "Category" });
            routes.MapRoute("Default", "{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional });
        }
    }
}
