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

namespace PharmMgtSys.Controllers
{
    public class PurchasesController : Controller
    {
        private PharmacyContext db = new PharmacyContext();

        // GET: Purchases
        public async Task<ActionResult> Index()
        {
            var purchases = db.Purchases.Include(p => p.Medication);
            return View(await purchases.ToListAsync());
        }

        // GET: Purchases/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Purchase purchase = await db.Purchases.FindAsync(id);
            if (purchase == null)
            {
                return HttpNotFound();
            }
            return View(purchase);
        }

        // GET: Purchases/Create
        public ActionResult Create()
        {
            ViewBag.MedicationID = new SelectList(db.Medications, "MedicatinID", "Name");
            return View();
        }

        // POST: Purchases/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "PurchaseID,PurchaseDate,MedicationID,Quantity")] Purchase purchase)
        {
            if (ModelState.IsValid)
            {
                purchase.PurchaseDate = DateTime.Now; //Set current date
                db.Purchases.Add(purchase);

                // Increase stock
                var medication = db.Medications.Find(purchase.Medication);
                if (medication != null)
                {
                    medication.QuantityInStock += purchase.Quantity;
                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MedicationID = new SelectList(db.Medications, "MedicatinID", "Name", purchase.MedicationID);
            return View(purchase);
        }

        // GET: Purchases/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Purchase purchase = await db.Purchases.FindAsync(id);
            if (purchase == null)
            {
                return HttpNotFound();
            }
            ViewBag.MedicationID = new SelectList(db.Medications, "MedicatinID", "Name", purchase.MedicationID);
            return View(purchase);
        }

        // POST: Purchases/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "PurchaseID,PurchaseDate,MedicationID,Quantity")] Purchase purchase)
        {
            if (ModelState.IsValid)
            {
                db.Entry(purchase).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.MedicationID = new SelectList(db.Medications, "MedicatinID", "Name", purchase.MedicationID);
            return View(purchase);
        }

        // GET: Purchases/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Purchase purchase = await db.Purchases.FindAsync(id);
            if (purchase == null)
            {
                return HttpNotFound();
            }
            return View(purchase);
        }

        // POST: Purchases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Purchase purchase = await db.Purchases.FindAsync(id);
            db.Purchases.Remove(purchase);
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
