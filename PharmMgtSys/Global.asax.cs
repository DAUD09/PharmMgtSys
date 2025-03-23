using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using PharmMgtSys.App_Start;
using PharmMgtSys.Models;

namespace PharmMgtSys
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            DevExtremeBundleConfig.RegisterBundles(BundleTable.Bundles);
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            CreateRolesAndUsers();

        }

        private void CreateRolesAndUsers()
        {
            using (var context = new ApplicationDbContext())
            {
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
                var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

                if (!roleManager.RoleExists("Admin"))
                {
                    roleManager.Create(new IdentityRole("Admin"));
                    System.Diagnostics.Debug.WriteLine("Admin role created");
                }
                if (!roleManager.RoleExists("Pharmacist"))
                {
                    roleManager.Create(new IdentityRole("Pharmacist"));
                    System.Diagnostics.Debug.WriteLine("Pharmacist role created");
                }
                if (userManager.FindByName("admin") == null)
                {
                    var admin = new ApplicationUser { UserName = "admin", Email = "admin@example.com" };
                    var result = userManager.Create(admin, "AdminPassword123!");
                    if (result.Succeeded)
                    {
                        userManager.AddToRole(admin.Id, "Admin");
                        System.Diagnostics.Debug.WriteLine("Admin user created and assigned to Admin role");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User creation failed: " + string.Join(", ", result.Errors));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Admin user already exists");
                }
            }
        }
    }
}

