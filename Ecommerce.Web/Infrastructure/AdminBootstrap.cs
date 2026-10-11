using System;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Ecommerce.Web.Models;

namespace Ecommerce.Web.Infrastructure
{
    // Opt-in administrator provisioning.
    // No administrator account exists unless BOTH ADMIN_EMAIL and ADMIN_PASSWORD are
    // supplied to the web process (docker-compose / IIS environment). When they are
    // absent the Admin role is never used and /Admin stays unreachable, so the public
    // site never gains a known, shared or default credential.
    public static class AdminBootstrap
    {
        public const string RoleName = "Admin";

        public static string Status { get; private set; }

        public static void Run()
        {
            try
            {
                var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
                var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

                if (string.IsNullOrWhiteSpace(email))
                {
                    Status = "skipped: ADMIN_EMAIL not set";
                    return;
                }
                if (string.IsNullOrWhiteSpace(password))
                {
                    Status = "skipped: ADMIN_PASSWORD not set";
                    return;
                }

                using (var db = ApplicationDbContext.Create())
                {
                    var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
                    if (!roleManager.RoleExists(RoleName))
                        roleManager.Create(new IdentityRole(RoleName));

                    var role = db.Roles.FirstOrDefault(r => r.Name == RoleName);
                    var userName = email.Trim();

                    var user = db.Users.FirstOrDefault(u => u.UserName == userName || u.Email == userName);
                    if (user == null)
                    {
                        // Same password policy as ApplicationUserManager.Create.
                        var users = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db))
                        {
                            PasswordValidator = new PasswordValidator
                            {
                                RequiredLength = 6,
                                RequireDigit = false,
                                RequireLowercase = false,
                                RequireUppercase = false,
                                RequireNonLetterOrDigit = false
                            }
                        };
                        user = new ApplicationUser
                        {
                            UserName = userName,
                            Email = userName,
                            FirstName = "Store",
                            LastName = "Administrator",
                            EmailConfirmed = true
                        };
                        var created = users.Create(user, password);
                        if (!created.Succeeded)
                        {
                            Status = "failed: " + string.Join(" ", created.Errors);
                            return;
                        }
                        Status = "created admin " + userName;
                    }

                    if (role != null && !db.Set<IdentityUserRole>().Any(r => r.UserId == user.Id && r.RoleId == role.Id))
                    {
                        db.Set<IdentityUserRole>().Add(new IdentityUserRole { UserId = user.Id, RoleId = role.Id });
                        db.SaveChanges();
                        Status = (Status == null ? "" : Status + "; ") + "assigned role " + RoleName;
                    }

                    if (Status == null)
                        Status = "admin " + userName + " already provisioned";
                }
            }
            catch (Exception ex)
            {
                // Never block application start-up for an optional admin account.
                Status = "error: " + ex.Message;
            }
        }
    }
}
