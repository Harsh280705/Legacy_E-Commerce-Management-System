using System.Web.Mvc;
using Ecommerce.Core.Interfaces;
using Ecommerce.Data;
using Ecommerce.Services.Impl;
using Ecommerce.Web.Infrastructure;
using Unity;
using Unity.Lifetime;

namespace Ecommerce.Web
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();
            // Child container per request (see UnityResolver) => per-request instances.
            container.RegisterType<EcommerceDbContext>(new HierarchicalLifetimeManager());
            container.RegisterType<ICatalogService, CatalogService>(new HierarchicalLifetimeManager());
            container.RegisterType<ICartService, CartService>(new HierarchicalLifetimeManager());
            container.RegisterType<ICheckoutService, CheckoutService>(new HierarchicalLifetimeManager());
            container.RegisterType<IAccountService, AccountService>(new HierarchicalLifetimeManager());
            container.RegisterType<IDatabaseHealthCheck, DatabaseHealthCheck>(new HierarchicalLifetimeManager());
            container.RegisterType<IAdminService, AdminService>(new HierarchicalLifetimeManager());
            DependencyResolver.SetResolver(new UnityResolver(container));
        }
    }
}
