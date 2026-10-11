using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Unity;

namespace Ecommerce.Web.Infrastructure
{
    /// <summary>
    /// Unity resolver for ASP.NET MVC 5 (whose IDependencyResolver has no scopes).
    /// Per-request lifetime is provided by a child container stored in
    /// HttpContext.Items and disposed null-safely in Application_EndRequest.
    /// (Unity.Mvc5's RequestLifetimeHttpModule throws NRE at teardown for async
    /// actions under Mono/XSP, so the composition root uses this instead while
    /// still resolving everything through Unity.)
    /// </summary>
    public class UnityResolver : IDependencyResolver
    {
        internal const string ChildKey = "LE_UnityChild";
        private readonly IUnityContainer _root;
        public UnityResolver(IUnityContainer root) { _root = root; }

        private IUnityContainer ChildContainer
        {
            get
            {
                var ctx = HttpContext.Current;
                if (ctx == null) return _root;
                var child = ctx.Items[ChildKey] as IUnityContainer;
                if (child == null)
                {
                    child = _root.CreateChildContainer();
                    ctx.Items[ChildKey] = child;
                }
                return child;
            }
        }

        public object GetService(Type serviceType)
        {
            try { return ChildContainer.Resolve(serviceType); }
            catch { return null; }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try { return ChildContainer.ResolveAll(serviceType); }
            catch { return new List<object>(); }
        }

        public static void DisposeChildContainer()
        {
            try
            {
                var ctx = HttpContext.Current;
                if (ctx == null) return;
                var child = ctx.Items[ChildKey] as IUnityContainer;
                ctx.Items[ChildKey] = null;
                if (child != null) child.Dispose();
            }
            catch { /* never fail request teardown */ }
        }
    }
}
