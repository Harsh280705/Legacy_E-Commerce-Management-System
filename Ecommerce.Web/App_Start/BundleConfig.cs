using System.Web.Optimization;

namespace Ecommerce.Web
{
    public class BundleConfig
    {
        // Legacy stack: no build pipeline. Bundles serve local jQuery 3.4.1 / Bootstrap 3.4.1 copies.
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include("~/Scripts/jquery-3.4.1.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.validate.min.js", "~/Scripts/jquery.validate.unobtrusive.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include("~/Scripts/bootstrap.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/site").Include("~/Scripts/site.js"));
            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/bootstrap.min.css", "~/Content/site.css"));
        }
    }
}
