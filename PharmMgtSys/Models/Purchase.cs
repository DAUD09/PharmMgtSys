using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmMgtSys.Models
{
	public class Purchase 
    {

		[Key]
		public int PurchaseID { get; set; }

		[Display(Name = "Purchase Date")]
		[DataType(DataType.Date)]
		public DateTime PurchaseDate { get; set; }

		[ForeignKey("Medication")]
		public int MedicationID { get; set; }

		[Range(1, int.MaxValue)]
		public int Quantity { get; set; }

		public virtual Medication Medication { get; set; }


	}
}