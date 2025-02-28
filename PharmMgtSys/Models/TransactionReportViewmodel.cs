using System;

namespace PharmMgtSys.Models
{
	public class TransactionReportViewModel
	{

		public DateTime Date { get; set; }

		public string TransactionType { get; set; } //Purchase or sale

		public string MedicationName { get; set; }

		public int Quantiity { get; set; }

		public decimal? Price { get; set; } // Nullable, only for sales
	}
}