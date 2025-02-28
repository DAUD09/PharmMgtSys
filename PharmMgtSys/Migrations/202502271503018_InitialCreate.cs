namespace PharmMgtSys.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Medications",
                c => new
                    {
                        MedicatinID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        QuantityInStock = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MedicatinID);
            
            CreateTable(
                "dbo.Purchases",
                c => new
                    {
                        PurchaseID = c.Int(nullable: false, identity: true),
                        PurchaseDate = c.DateTime(nullable: false),
                        MedicationID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PurchaseID)
                .ForeignKey("dbo.Medications", t => t.MedicationID, cascadeDelete: true)
                .Index(t => t.MedicationID);
            
            CreateTable(
                "dbo.Sales",
                c => new
                    {
                        SaleID = c.Int(nullable: false, identity: true),
                        SaleDate = c.DateTime(nullable: false),
                        MedicationID = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.SaleID)
                .ForeignKey("dbo.Medications", t => t.MedicationID, cascadeDelete: true)
                .Index(t => t.MedicationID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Sales", "MedicationID", "dbo.Medications");
            DropForeignKey("dbo.Purchases", "MedicationID", "dbo.Medications");
            DropIndex("dbo.Sales", new[] { "MedicationID" });
            DropIndex("dbo.Purchases", new[] { "MedicationID" });
            DropTable("dbo.Sales");
            DropTable("dbo.Purchases");
            DropTable("dbo.Medications");
        }
    }
}
