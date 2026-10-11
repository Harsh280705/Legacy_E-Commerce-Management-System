using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(Ecommerce.Web.Startup))]

namespace Ecommerce.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.CreatePerOwinContext(Models.ApplicationDbContext.Create);
            app.CreatePerOwinContext<Models.ApplicationUserManager>(Models.ApplicationUserManager.Create);
            app.CreatePerOwinContext<Models.ApplicationSignInManager>(Models.ApplicationSignInManager.Create);
            // Registers the Identity role manager so role claims land in the auth
            // cookie (used only for UI affordances such as the Admin nav link;
            // access control itself is enforced server-side in AdminController).
            app.CreatePerOwinContext<RoleManager<IdentityRole>>((options, context) =>
                new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context.Get<Models.ApplicationDbContext>())));
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                ExpireTimeSpan = System.TimeSpan.FromDays(7),
                SlidingExpiration = true,
                CookieHttpOnly = true
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
        }
    }
}
