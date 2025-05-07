using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.SignalR;
using PharmMgtSys.App_Start;
using PharmMgtSys.Hubs;
using PharmMgtSys.Models;

namespace PharmMgtSys
{

    public class MvcApplication : System.Web.HttpApplication
    {
        private static Timer stockAlertTimer;
        protected void Application_Start()
        {
            DevExtremeBundleConfig.RegisterBundles(BundleTable.Bundles);
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            CreateRolesAndUsers();


            stockAlertTimer = new Timer(60000); // Check every minute
            stockAlertTimer.Elapsed += CheckStockLevels;
            stockAlertTimer.AutoReset = true;
            stockAlertTimer.Start();

        }

        private void CheckStockLevels(object sender, ElapsedEventArgs e)
        {
            using (var context = new ApplicationDbContext())
            {
                var lowStockItems = context.Medications
                    .Where(m => m.QuantityInStock < m.ReorderLevel)
                    .ToList();
                System.Diagnostics.Debug.WriteLine($"Found {lowStockItems.Count} low stock items at {DateTime.Now}");

                foreach (var medication in lowStockItems)
                {
                    System.Diagnostics.Debug.WriteLine($"Sending alert for {medication.Name}");
                    NotificationService.SendStockAlert(medication);
                }
            }
        }

        public static class NotificationService
        {
            public static void SendStockAlert(Medication medication)
            {
                using (var context = new ApplicationDbContext())
                {
                    // Calculate the threshold outside the LINQ query
                    var threshold = DateTime.UtcNow.AddHours(-1);

                    // Check if a similar notification exists within the last hour
                    var recentNotificationExists = context.Notifications
                        .Any(n => n.Message.Contains(medication.Name) &&
                                  n.Timestamp > threshold);

                    if (!recentNotificationExists)
                    {
                        var notification = new Notification
                        {
                            UserId = "admin", // Replace with dynamic logic later
                            Message = $"Stock alert: {medication.Name} has {medication.QuantityInStock} units, below reorder level of {medication.ReorderLevel}.",
                            IsRead = false,
                            Timestamp = DateTime.UtcNow
                        };
                        context.Notifications.Add(notification);
                        context.SaveChanges();

                        // Push via SignalR
                        var hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                        hubContext.Clients.User("admin").addNotification(notification.Message);
                    }
                }
            }
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

