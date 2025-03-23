using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace PharmMgtSys.Models
{
	public class Sale
	{

		[Key]
		public int SaleID { get; set; }

		[Display(Name = "Sale Date")]
		[DataType(DataType.Date)]
		public DateTime SaleDate { get; set; }

		[ForeignKey("Medication")]
		public int MedicationID { get; set; }

		[Range(1, int.MaxValue)]
		public int Quantity { get; set; }

		[Range(1, double.MaxValue)]
		public decimal Price { get; set; }

		public virtual Medication Medication { get; set; }
	}
}