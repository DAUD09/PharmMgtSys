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
    [Route("api/AuditLogsWeb/{action}", Name = "AuditLogsWebApi")]
    public class AuditLogsWebApiController : ApiController
    {
        private ApplicationDbContext _context = new ApplicationDbContext();

        [HttpGet]
        public async Task<HttpResponseMessage> Get(DataSourceLoadOptions loadOptions)
        {
            var auditlogs = from log in _context.AuditLogs
                            join user in _context.Users on log.UserId equals user.Id into userJoin
                            from user in userJoin.DefaultIfEmpty()
                            select new
                            {
                                log.Id,
                                UserEmail = user != null ? user.Email : "System",
                                log.Action,
                                log.EntityName,
                                log.EntityId,
                                log.Timestamp,
                                log.Details
                            };

            return Request.CreateResponse(await DataSourceLoader.LoadAsync(auditlogs, loadOptions));
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post(FormDataCollection form)
        {
            var model = new AuditLog();
            var values = JsonConvert.DeserializeObject<IDictionary>(form.Get("values"));
            PopulateModel(model, values);

            Validate(model);
            if (!ModelState.IsValid)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, GetFullErrorMessage(ModelState));

            var result = _context.AuditLogs.Add(model);
            await _context.SaveChangesAsync();

            return Request.CreateResponse(HttpStatusCode.Created, new { result.Id });
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Put(FormDataCollection form)
        {
            var key = Convert.ToInt32(form.Get("key"));
            var model = await _context.AuditLogs.FirstOrDefaultAsync(item => item.Id == key);
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
            var model = await _context.AuditLogs.FirstOrDefaultAsync(item => item.Id == key);

            _context.AuditLogs.Remove(model);
            await _context.SaveChangesAsync();
        }

        private void PopulateModel(AuditLog model, IDictionary values)
        {
            string ID = nameof(AuditLog.Id);
            string USER_ID = nameof(AuditLog.UserId);
            string ACTION = nameof(AuditLog.Action);
            string ENTITY_NAME = nameof(AuditLog.EntityName);
            string ENTITY_ID = nameof(AuditLog.EntityId);
            string TIMESTAMP = nameof(AuditLog.Timestamp);
            string DETAILS = nameof(AuditLog.Details);

            if (values.Contains(ID))
            {
                model.Id = Convert.ToInt32(values[ID]);
            }

            if (values.Contains(USER_ID))
            {
                model.UserId = Convert.ToString(values[USER_ID]);
            }

            if (values.Contains(ACTION))
            {
                model.Action = Convert.ToString(values[ACTION]);
            }

            if (values.Contains(ENTITY_NAME))
            {
                model.EntityName = Convert.ToString(values[ENTITY_NAME]);
            }

            if (values.Contains(ENTITY_ID))
            {
                model.EntityId = Convert.ToString(values[ENTITY_ID]);
            }

            if (values.Contains(TIMESTAMP))
            {
                model.Timestamp = Convert.ToDateTime(values[TIMESTAMP]);
            }

            if (values.Contains(DETAILS))
            {
                model.Details = Convert.ToString(values[DETAILS]);
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