using System.ComponentModel.DataAnnotations;

namespace PharmMgtSys.Models
{
	public class Medication
	{

		[Key]
		public int MedicatinID { get; set; }

		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		[Range(0, double.MaxValue)]
		public decimal Price { get; set; }

		[Range(0, int.MaxValue)]
		public int QuantityInStock { get; set; }
	}
}