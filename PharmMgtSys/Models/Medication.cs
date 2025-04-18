using System.ComponentModel.DataAnnotations;

namespace PharmMgtSys.Models
{
	public class Medication
	{

		[Key]
		public int MedicationID { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Range(0, double.MaxValue)]
		public decimal Price { get; set; }

		[Display(Name = "Quantity In Stock")]
		[Range(0, int.MaxValue)]
		public int QuantityInStock { get; set; }

		public int ReorderLevel { get; set; }
	}
}