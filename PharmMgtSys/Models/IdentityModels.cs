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
    public interface IAuditable { } // Keep interface for potential future use

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
        public DbSet<Medication> Medications { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public override int SaveChanges()
        {
            var auditLogs = new List<AuditLog>();
            var currentUserId = HttpContext.Current?.User?.Identity?.GetUserId() ?? "System";

            // Track changes for all entities
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted);

            if (!entries.Any())
            {
                Debug.WriteLine($"No entities found in ChangeTracker for auditing at {DateTime.UtcNow}");
            }

            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var action = entry.State.ToString();
                var entityName = entity.GetType().Name;
                var entityId = GetEntityId(entry);
                var details = GetDetails(entry);

                var auditLog = new AuditLog
                {
                    UserId = currentUserId,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Timestamp = DateTime.UtcNow,
                    Details = details
                };
                auditLogs.Add(auditLog);

                Debug.WriteLine($"Audit log created: Entity={entityName}, Action={action}, EntityId={entityId}, Timestamp={DateTime.UtcNow}");
            }

            // Add audit logs to the context before saving
            if (auditLogs.Any())
            {
                Debug.WriteLine($"Preparing to save {auditLogs.Count} audit logs at {DateTime.UtcNow}");
                AuditLogs.AddRange(auditLogs);
            }

            try
            {
                // Save both entity changes and audit logs in a single transaction
                int result = base.SaveChanges();
                Debug.WriteLine("All changes (entities and audit logs) saved successfully");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving changes: {ex.Message}, InnerException: {ex.InnerException?.Message}");
                throw; // Re-throw to ensure the caller handles the error
            }
        }

        private string GetEntityId(DbEntityEntry entry)
        {
            var entity = entry.Entity;
            var property = entity.GetType().GetProperty("Id");
            if (property != null)
            {
                var value = property.GetValue(entity);
                return value?.ToString() ?? "Unknown";
            }
            return "Unknown";
        }

        private string GetDetails(DbEntityEntry entry)
        {
            if (entry.State == EntityState.Added)
            {
                return JsonConvert.SerializeObject(entry.CurrentValues.ToObject());
            }
            else if (entry.State == EntityState.Modified)
            {
                var original = entry.OriginalValues.ToObject();
                var current = entry.CurrentValues.ToObject();
                return $"Original: {JsonConvert.SerializeObject(original)}, Current: {JsonConvert.SerializeObject(current)}";
            }
            else if (entry.State == EntityState.Deleted)
            {
                return "Entity Deleted";
            }
            return "No details available";
        }
    }
}