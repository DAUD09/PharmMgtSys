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
    [Route("api/SalesWebApi/{action}", Name = "SalesWebApi")]
    public class SalesWebApiController : ApiController
    {
        private ApplicationDbContext _context = new ApplicationDbContext();

        [HttpGet]
        public async Task<HttpResponseMessage> Get(DataSourceLoadOptions loadOptions)
        {
            var sales = _context.Sales
                .Include(s => s.Medication)
                .Select(i => new {
                    i.SaleID,
                    i.SaleDate,
                    i.MedicationID,
                    i.Quantity,
                    i.Price,
                    Medication = new
                    {
                        i.Medication.MedicationID,
                        i.Medication.Name,
                        i.Medication.QuantityInStock,
                        i.Medication.Price
                    }
                });

            var result = await DataSourceLoader.LoadAsync(sales, loadOptions);
            return Request.CreateResponse(result);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetAggregatedSales()
        {
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var aggregatedSales = await _context.Sales
                .Where(s => s.SaleDate >= sevenDaysAgo)
                .GroupBy(s => s.Medication.Name)
                .Select(g => new
                {
                    MedicationName = g.Key,
                    TotalQuantity = g.Sum(s => s.Quantity)
                })
                .ToListAsync();

            return Request.CreateResponse(aggregatedSales);
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(FormDataCollection form)
        {
            var model = new Sale();
            var values = JsonConvert.DeserializeObject<IDictionary>(form.Get("values"));
            PopulateModel(model, values);

            Validate(model);
            if (!ModelState.IsValid)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, GetFullErrorMessage(ModelState));

            // Check stock availability
            var medication = await _context.Medications.FindAsync(model.MedicationID);
            if (medication == null || medication.QuantityInStock < model.Quantity)
            {
                var errorMsg = "Insufficient stock for " + (medication?.Name ?? "unknown medication");
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, errorMsg);
            }

            // Set current date
            model.SaleDate = DateTime.Now;

            // Optionally set price from medication if needed
            //model.Price = medication.Price ;

            // Save the sale
            _context.Sales.Add(model);

            // Decrease stock
            medication.QuantityInStock -= model.Quantity;

            await _context.SaveChangesAsync();

            return Request.CreateResponse(HttpStatusCode.Created, new { model.SaleID });
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Put(FormDataCollection form)
        {
            var key = Convert.ToInt32(form.Get("key"));
            var model = await _context.Sales.FirstOrDefaultAsync(item => item.SaleID == key);
            if (model == null)
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
        public async Task Delete(FormDataCollection form)
        {
            var key = Convert.ToInt32(form.Get("key"));
            var model = await _context.Sales.FirstOrDefaultAsync(item => item.SaleID == key);

            _context.Sales.Remove(model);
            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<HttpResponseMessage> MedicationsLookup(DataSourceLoadOptions loadOptions)
        {
            var lookup = from i in _context.Medications
                         orderby i.Name
                         select new
                         {
                             Value = i.MedicationID, // Keep as Value for binding
                             Text = i.Name,        // Keep as Text for display in dropdown

                             // Add the extra data needed in the form
                             QuantityInStock = i.QuantityInStock,
                             UnitPrice = i.Price // Use a clear name like UnitPrice
                         };
            // Ensure DataSourceLoader works with the anonymous type including new fields
            var loadResult = await DataSourceLoader.LoadAsync(lookup, loadOptions);
            return Request.CreateResponse(loadResult);
        }

        private void PopulateModel(Sale model, IDictionary values)
        {
            string SALE_ID = nameof(Sale.SaleID);
            string SALE_DATE = nameof(Sale.SaleDate);
            string MEDICATION_ID = nameof(Sale.MedicationID);
            string QUANTITY = nameof(Sale.Quantity);
            string PRICE = nameof(Sale.Price);

            if (values.Contains(SALE_ID))
            {
                model.SaleID = Convert.ToInt32(values[SALE_ID]);
            }

            if (values.Contains(SALE_DATE))
            {
                model.SaleDate = Convert.ToDateTime(values[SALE_DATE]);
            }

            if (values.Contains(MEDICATION_ID))
            {
                model.MedicationID = Convert.ToInt32(values[MEDICATION_ID]);
            }

            if (values.Contains(QUANTITY))
            {
                model.Quantity = Convert.ToInt32(values[QUANTITY]);
            }

            if (values.Contains(PRICE))
            {
                model.Price = Convert.ToDecimal(values[PRICE], CultureInfo.InvariantCulture);
            }
        }

        private string GetFullErrorMessage(ModelStateDictionary modelState)
        {
            var messages = new List<string>();

            foreach (var entry in modelState)
            {
                foreach (var error in entry.Value.Errors)
                    messages.Add(error.ErrorMessage);
            }

            return String.Join(" ", messages);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}