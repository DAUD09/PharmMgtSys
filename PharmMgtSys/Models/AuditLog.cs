using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PharmMgtSys.Models
{
	public class AuditLog
	{

		public int Id { get; set; }

		public string UserId { get; set; } // Links to the user who performed the action

		public string Action { get; set; } // "Insert", "Update", "Delete"

		public string EntityName { get; set; } // e.g., "Medication", "Sale"

		public string EntityId { get; set; } // The ID of the affected record

		public DateTime Timestamp { get; set; } // When the action occurred

		public string Details { get; set; } // Additional info (e.g., before/after values


	}


}