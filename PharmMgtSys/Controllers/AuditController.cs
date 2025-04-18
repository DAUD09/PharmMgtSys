using System.Linq;
using System.Web.Mvc;
using PharmMgtSys.Models;

namespace PharmMgtSys.Controllers
{
    [Authorize(Roles = "Admin")] // Restrict to admins only
    public class AuditController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Audit/Index


        public ActionResult Index()
        {
            var auditLogs = db.AuditLogs
                .OrderByDescending(l => l.Timestamp) // Most recent first
                .Take(100) // Limit to 100 entries for performance (optional)
                .ToList();
            return View(auditLogs);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}