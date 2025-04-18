using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PharmMgtSys.Models;

namespace PharmMgtSys.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult TestAudit()
        {
            using (var db = new ApplicationDbContext())
            {
                var med = db.Medications.First();
                med.QuantityInStock += 1;
                db.SaveChanges();
                return Content("Audit test complete");
            }
        }

        //public ActionResult TestStockAlert()
        //{
        //    using (var db = new PharmacyContext())
        //    {
        //        var med = db.Medications.FirstOrDefault(); // Get the first medication
        //        if (med != null)
        //        {
        //            med.ReorderLevel = 10; // Set a threshold
        //            med.QuantityInStock = 5; // Set below threshold to trigger alert
        //            db.SaveChanges();
        //            return Content("Stock alert test data set up. Wait 1 minute to check Notifications.");
        //        }
        //        return Content("No medications found to test.");
        //    }
        //}

        // Optional: For easy verification
        //public ActionResult CheckNotifications()
        //{
        //    using (var db = new PharmacyContext())
        //    {
        //        var notifications = db.Notifications.ToList();
        //        return Json(notifications, JsonRequestBehavior.AllowGet);
        //    }
        //}
    }

}
