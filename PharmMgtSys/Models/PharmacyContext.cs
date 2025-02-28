using System.Data.Entity;

namespace PharmMgtSys.Models
{
	public class PharmacyContext : DbContext
	{

		public PharmacyContext() : base("name=PharmacyConnectionString")
		{
		}

		public DbSet<Medication> Medications { get; set; }
		public DbSet<Purchase> Purchases { get; set; }
		public DbSet<Sale> Sales { get; set; }


	}
}