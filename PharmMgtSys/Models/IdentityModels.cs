using System.Data.Entity;
using MySql.Data.Entity;
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
using System.Diagnostics;

namespace PharmMgtSys.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true; // Default to active

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
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
            var currentUserId = HttpContext.Current?.User?.Identity?.GetUserId() ?? "System";

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

            if (auditLogs.Any())
            {
                Debug.WriteLine($"Adding {auditLogs.Count} audit logs at {DateTime.UtcNow}");
            }

            int result = base.SaveChanges();

            if (auditLogs.Any())
            {
                AuditLogs.AddRange(auditLogs);
                base.SaveChanges();
                Debug.WriteLine("Audit logs saved successfully");
            }

            return result;
        }

        private string GetEntityId(object entity)
        {
            var property = entity.GetType().GetProperty("Id");
            return property?.GetValue(entity)?.ToString() ?? "Unknown";
        }

        private string GetDetails(DbEntityEntry entry)
        {
            if (entry.Entity is Sale)
            {
                var sale = entry.Entity as Sale;
                return JsonConvert.SerializeObject(new
                {
                    sale.SaleDate,
                    sale.Price
                });
            }
            else if (entry.Entity is Purchase)
            {
                var purchase = entry.Entity as Purchase;
                return JsonConvert.SerializeObject(new
                {
                    purchase.PurchaseDate,
                    purchase.MedicationID
                });
            }
            else if (entry.State == EntityState.Added || entry.State == EntityState.Deleted)
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