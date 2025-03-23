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
    [Route("api/MedicationsWeb/{action}", Name = "MedicationsWebApi")]
    public class MedicationsWebApiController : ApiController
    {
        private PharmacyContext _context = new PharmacyContext();

        [HttpGet]
        public async Task<HttpResponseMessage> Get(DataSourceLoadOptions loadOptions) {
            var medications = _context.Medications.Select(i => new {
                i.MedicatinID,
                i.Name,
                i.Price,
                i.QuantityInStock
            });

            // If underlying data is a large SQL table, specify PrimaryKey and PaginateViaPrimaryKey.
            // This can make SQL execution plans more efficient.
            // For more detailed information, please refer to this discussion: https://github.com/DevExpress/DevExtreme.AspNet.Data/issues/336.
            // loadOptions.PrimaryKey = new[] { "MedicatinID" };
            // loadOptions.PaginateViaPrimaryKey = true;

            return Request.CreateResponse(await DataSourceLoader.LoadAsync(medications, loadOptions));
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(FormDataCollection form) {
            var model = new Medication();
            var values = JsonConvert.DeserializeObject<IDictionary>(form.Get("values"));
            PopulateModel(model, values);

            Validate(model);
            if (!ModelState.IsValid)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, GetFullErrorMessage(ModelState));

            var result = _context.Medications.Add(model);
            await _context.SaveChangesAsync();

            return Request.CreateResponse(HttpStatusCode.Created, new { result.MedicatinID });
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Put(FormDataCollection form) {
            var key = Convert.ToInt32(form.Get("key"));
            var model = await _context.Medications.FirstOrDefaultAsync(item => item.MedicatinID == key);
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
            var model = await _context.Medications.FirstOrDefaultAsync(item => item.MedicatinID == key);

            _context.Medications.Remove(model);
            await _context.SaveChangesAsync();
        }


        private void PopulateModel(Medication model, IDictionary values) {
            string MEDICATIN_ID = nameof(Medication.MedicatinID);
            string NAME = nameof(Medication.Name);
            string PRICE = nameof(Medication.Price);
            string QUANTITY_IN_STOCK = nameof(Medication.QuantityInStock);

            if(values.Contains(MEDICATIN_ID)) {
                model.MedicatinID = Convert.ToInt32(values[MEDICATIN_ID]);
            }

            if(values.Contains(NAME)) {
                model.Name = Convert.ToString(values[NAME]);
            }

            if(values.Contains(PRICE)) {
                model.Price = Convert.ToDecimal(values[PRICE], CultureInfo.InvariantCulture);
            }

            if(values.Contains(QUANTITY_IN_STOCK)) {
                model.QuantityInStock = Convert.ToInt32(values[QUANTITY_IN_STOCK]);
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