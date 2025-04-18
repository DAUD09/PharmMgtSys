using System.Data.Entity;
using  MySql.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System;

namespace PharmMgtSys.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("MysqlData", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public override int SaveChanges()
        {
            var auditLogs = new List<AuditLog>();
            // Get the current user's ID (assuming ASP.NET Identity)
            var currentUserId = HttpContext.Current?.User?.Identity?.GetUserId() ?? "System";

            // Loop through all tracked entities that have changed
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            {
                var auditLog = new AuditLog
                {
                    UserId = currentUserId,
                    Action = entry.State.ToString(),
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = GetEntityId(entry.Entity),
                    Timestamp = DateTime.UtcNow,
                    Details = GetDetails(entry)
                };
                auditLogs.Add(auditLog);
            }

            // Save the original changes first
            int result = base.SaveChanges();

            // Then save the audit logs
            if (auditLogs.Any())
            {
                AuditLogs.AddRange(auditLogs);
                base.SaveChanges(); // Second save for audit logs
            }

            return result;
        }

        // Helper method to get the entity's ID
        private string GetEntityId(object entity)
        {
            var property = entity.GetType().GetProperty("Id");
            return property?.GetValue(entity)?.ToString() ?? "Unknown";
        }

        // Helper method to capture details of the change
        private string GetDetails(DbEntityEntry entry)
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
            {
                return JsonConvert.SerializeObject(entry.Entity);
            }
            else if (entry.State == EntityState.Modified)
            {
                var original = entry.OriginalValues.ToObject();
                var current = entry.CurrentValues.ToObject();
                return $"Original: {JsonConvert.SerializeObject(original)}, Current: {JsonConvert.SerializeObject(current)}";
            }
            return null;
        }

        public DbSet<Medication> Medications { get; set; }

        public DbSet<Purchase> Purchases { get; set; }

        public DbSet<Sale> Sales { get; set; }

        public DbSet<Notification> Notifications { get; set; }

    }
}