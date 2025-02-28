using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using PharmMgtSys.Models;

namespace PharmMgtSys.Controllers
{
    public class ReportsController : Controller
    {

        private PharmacyContext db = new PharmacyContext();


        // GET: Stock Report
        public ActionResult StockReport()
        {

            var stockReport = db.Medications.Select(m => new StockReportViewModel
            {
                MedicationName = m.Name,
                QuantityInStock = m.QuantityInStock
            }).ToList();


            return View(stockReport);
        }

        //Get: Transaction Report (with filter)
        public ActionResult TransactionReport(TransactionReportFilterViewModel filter = null)
        {
            // Default to last #) days if no filter provided 
            var endDate = filter?.EndDate ?? DateTime.Now;
            var startDate = filter?.StartDate ?? endDate.AddDays(-30);

            // Fetch purchases
            var purchases = db.Purchases.Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate).Select(p => new TransactionReportViewModel
            {

                Date = p.PurchaseDate,
                TransactionType = "Purchase",
                MedicationName = p.Medication.Name,
                Quantiity = p.Quantity,
                Price = null // No price for purchases
            });

            // Fetch Sales
            var sales = db.Sales.Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate).Select(s => new TransactionReportViewModel
            {

                Date = s.SaleDate,
                TransactionType = "Sale",
                MedicationName = s.Medication.Name,
                Quantiity = s.Quantity,
                Price = s.Price
            });

            // Combine and order by date
            var transactions = purchases.Union(sales).OrderBy(t => t.Date).ToList();

            // Pass filter model to view for display
            ViewBag.Filter = new TransactionReportFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            return View(transactions);

        }

        // Cleanup
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