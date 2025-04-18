using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PharmMgtSys.Models;
using System.Net.Http.Headers;

namespace PharmMgtSys.Controllers
{
    [Authorize(Roles = "Pharmacist, Admin")]
    public class SalesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Sales
        public async Task<ActionResult> Index()
        {
            var sales = db.Sales.Include(s => s.Medication);
            return View(await sales.ToListAsync());
        }

        // GET: Sales/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Sale sale = await db.Sales.FindAsync(id);
            if (sale == null)
            {
                return HttpNotFound();
            }
            return View(sale);
        }

        // GET: Sales/Create
        public ActionResult Create()
        {
            ViewBag.MedicationID = new SelectList(db.Medications, "MedicationID", "Name");
            return View();
        }

        // POST: Sales/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "SaleID,SaleDate,MedicationID,Quantity,Price")] Sale sale)
        {
            if (ModelState.IsValid)
            {
                // Check stock Availability
                var medication = db.Medications.Find(sale.MedicationID);
                if (medication == null || medication.QuantityInStock < sale.Quantity)
                {
                    ModelState.AddModelError("", "Insuficient stock for " + (medication?.Name ?? "unkown medication"));
                    ViewBag.MedicationID = new SelectList(db.Medications, "MedicationID", "Name", sale.MedicationID);
                    return View(sale);
                }

                sale.SaleDate = DateTime.Now; // Set current date
                /*sale.Price = medication.Price;*/ // Set price from medication
                db.Sales.Add(sale);

                // Decrease stock
                medication.QuantityInStock -= sale.Quantity;

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MedicationID = new SelectList(db.Medications, "MedicationID", "Name", sale.MedicationID);
            var medications = await db.Medications.ToListAsync();
            return View(sale);
        }

        // GET: Sales/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Sale sale = await db.Sales.FindAsync(id);
            if (sale == null)
            {
                return HttpNotFound();
            }
            ViewBag.MedicationID = new SelectList(db.Medications, "MedicationID", "Name", sale.MedicationID);
            return View(sale);
        }

        // POST: Sales/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "SaleID,SaleDate,MedicationID,Quantity,Price")] Sale sale)
        {
            if (ModelState.IsValid)
            {
                db.Entry(sale).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.MedicationID = new SelectList(db.Medications, "MedicationID", "Name", sale.MedicationID);
            return View(sale);
        }

        // GET: Sales/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Sale sale = await db.Sales.FindAsync(id);
            if (sale == null)
            {
                return HttpNotFound();
            }
            return View(sale);
        }

        // POST: Sales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Sale sale = await db.Sales.FindAsync(id);
            db.Sales.Remove(sale);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
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
