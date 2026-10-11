using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Ecommerce.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            UnityConfig.RegisterComponents();
            Infrastructure.AdminBootstrap.Run();
        }

        protected void Application_EndRequest()
        {
            // Null-safe per-request Unity child-container disposal.
            Infrastructure.UnityResolver.DisposeChildContainer();
        }

        protected void Application_Error()
        {
            // Prevent leaking stack traces to visitors; Error view is shown via customErrors.
            var ex = Server.GetLastError();
            if (ex != null)
                System.Diagnostics.Trace.TraceError("Unhandled: " + ex.Message);
        }
    }
}
