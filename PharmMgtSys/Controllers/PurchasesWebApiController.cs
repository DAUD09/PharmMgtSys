using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using PharmMgtSys.Models;

namespace PharmMgtSys.Controllers
{
    [Authorize(Roles = "Pharmacist, Admin")]
    [Route("api/PurchasesWebApi/{action}", Name = "PurchasesWebApi")]
    public class PurchasesWebApiController : ApiController
    {
        private ApplicationDbContext _context = new ApplicationDbContext();

        [HttpGet]
        public async Task<HttpResponseMessage> Get(DataSourceLoadOptions loadOptions) {
            var purchases = _context.Purchases.Select(i => new {
                i.PurchaseID,
                i.PurchaseDate,
                i.MedicationID,
                i.Quantity
            });

            // If underlying data is a large SQL table, specify PrimaryKey and PaginateViaPrimaryKey.
            // This can make SQL execution plans more efficient.
            // For more detailed information, please refer to this discussion: https://github.com/DevExpress/DevExtreme.AspNet.Data/issues/336.
            // loadOptions.PrimaryKey = new[] { "PurchaseID" };
            // loadOptions.PaginateViaPrimaryKey = true;

            return Request.CreateResponse(await DataSourceLoader.LoadAsync(purchases, loadOptions));
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(FormDataCollection form)
        {
            var model = new Purchase();
            var values = JsonConvert.DeserializeObject<IDictionary>(form.Get("values"));
            PopulateModel(model, values);

            Validate(model);
            if (!ModelState.IsValid)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, GetFullErrorMessage(ModelState));

            // Set current date for the purchase.
            model.PurchaseDate = DateTime.Now;

            // Increase stock by finding the related medication.
            var medication = await _context.Medications.FindAsync(model.MedicationID);
            if (medication != null)
            {
                medication.QuantityInStock += model.Quantity;
            }
            else
            {
                // Return an error if the medication is not found.
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Medication not found");
            }

            // Save the purchase record.
            _context.Purchases.Add(model);
            await _context.SaveChangesAsync();

            return Request.CreateResponse(HttpStatusCode.Created, new { model.PurchaseID });
        }


        [HttpPut]
        public async Task<HttpResponseMessage> Put(FormDataCollection form) {
            var key = Convert.ToInt32(form.Get("key"));
            var model = await _context.Purchases.FirstOrDefaultAsync(item => item.PurchaseID == key);
            if(model == null)
                return Request.CreateResponse(HttpStatusCode.Conflict, "Object not found");

            var values = JsonConvert.DeserializeObject<IDictionary>(form.Get("values"));
            PopulateModel(model, values);

            Validate(model);
            if (!ModelState.IsValid)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, GetFullErrorMessage(ModelState));

            await _context.SaveChangesAsync();

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpDelete]
        public async Task Delete(FormDataCollection form) {
            var key = Convert.ToInt32(form.Get("key"));
            var model = await _context.Purchases.FirstOrDefaultAsync(item => item.PurchaseID == key);

            _context.Purchases.Remove(model);
            await _context.SaveChangesAsync();
        }


        [HttpGet]
        public async Task<HttpResponseMessage> MedicationsLookup(DataSourceLoadOptions loadOptions) {
            var lookup = from i in _context.Medications
                         orderby i.Name
                         select new {
                             Value = i.MedicationID,
                             Text = i.Name
                         };
            return Request.CreateResponse(await DataSourceLoader.LoadAsync(lookup, loadOptions));
        }

        private void PopulateModel(Purchase model, IDictionary values) {
            string PURCHASE_ID = nameof(Purchase.PurchaseID);
            string PURCHASE_DATE = nameof(Purchase.PurchaseDate);
            string MEDICATION_ID = nameof(Purchase.MedicationID);
            string QUANTITY = nameof(Purchase.Quantity);

            if(values.Contains(PURCHASE_ID)) {
                model.PurchaseID = Convert.ToInt32(values[PURCHASE_ID]);
            }

            if(values.Contains(PURCHASE_DATE)) {
                model.PurchaseDate = Convert.ToDateTime(values[PURCHASE_DATE]);
            }

            if(values.Contains(MEDICATION_ID)) {
                model.MedicationID = Convert.ToInt32(values[MEDICATION_ID]);
            }

            if(values.Contains(QUANTITY)) {
                model.Quantity = Convert.ToInt32(values[QUANTITY]);
            }
        }

        private string GetFullErrorMessage(ModelStateDictionary modelState) {
            var messages = new List<string>();

            foreach(var entry in modelState) {
                foreach(var error in entry.Value.Errors)
                    messages.Add(error.ErrorMessage);
            }

            return String.Join(" ", messages);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}