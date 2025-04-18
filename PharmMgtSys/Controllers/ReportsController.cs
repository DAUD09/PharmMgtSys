using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using PharmMgtSys.Models;

namespace PharmMgtSys.Controllers
{
    public class ReportsController : Controller
    {

        private ApplicationDbContext db = new ApplicationDbContext();


        // GET: Stock Report
        public ActionResult StockReport()
        {

            var stockReport = db.Medications.Select(m => new StockReportViewModel
            {
                MedicationName = m.Name,
                QuantityInStock = m.QuantityInStock
            }).ToList();


            return View(stockReport);
        }

        //Get: Transaction Report (with filter)
        public ActionResult TransactionReport(TransactionReportFilterViewModel filter = null)
        {
            // Default to last #) days if no filter provided 
            var endDate = filter?.EndDate ?? DateTime.Now;
            var startDate = filter?.StartDate ?? endDate.AddDays(-30);

            // Fetch purchases
            var purchases = db.Purchases.Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate).Select(p => new TransactionReportViewModel
            {

                Date = p.PurchaseDate,
                TransactionType = "Purchase",
                MedicationName = p.Medication.Name,
                Quantity = p.Quantity,
                Price = null // No price for purchases
            });

            // Fetch Sales
            var sales = db.Sales.Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate).Select(s => new TransactionReportViewModel
            {

                Date = s.SaleDate,
                TransactionType = "Sale",
                MedicationName = s.Medication.Name,
                Quantity = s.Quantity,
                Price = s.Price
            });

            // Combine and order by date
            var transactions = purchases.Union(sales).OrderBy(t => t.Date).ToList();

            // Pass filter model to view for display
            ViewBag.Filter = new TransactionReportFilterViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            return View(transactions);

        }

        // Export to PDF for Stock Report
        public ActionResult ExportStockReportToPDF()
        {

            var stockReport = db.Medications.Select(m => new StockReportViewModel
            {
                MedicationName = m.Name,
                QuantityInStock = m.QuantityInStock
            }).ToList();

            using (MemoryStream ms = new MemoryStream())
            {

                Document pdfDoc = new Document(PageSize.A4, 25, 25, 25, 25);
                PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                pdfDoc.Add(new Paragraph("Stock report", FontFactory.GetFont("Arial", 16, Font.BOLD)));
                pdfDoc.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));
                pdfDoc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(2);
                table.WidthPercentage = 100;
                table.AddCell("Medication Name");
                table.AddCell("Quantity in Stock");

                foreach (var item in stockReport)
                {

                    table.AddCell(item.MedicationName);
                    table.AddCell(item.QuantityInStock.ToString());
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(ms.ToArray(), "application/pdf", "StockReport.pdf");

            }
        }

        // Export to Excel for Stock Report
        public ActionResult ExportStockReportToExcel()
        {

            var stockReport = db.Medications.Select(m => new StockReportViewModel
            {

                MedicationName = m.Name,
                QuantityInStock = m.QuantityInStock
            }).ToList();

            using (var package = new ExcelPackage())
            {

                var worksheet = package.Workbook.Worksheets.Add("Stock Report");
                worksheet.Cells[1, 1].Value = "Stock Report";
                worksheet.Cells[1, 1, 1, 2].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 1].Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}";
                worksheet.Cells[3, 1].LoadFromCollection(stockReport, true);
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StockReport.xlsx");

            }
        }

        // New Export to PDF for Transaction Report
        public ActionResult ExportTransactionReportToPdf(TransactionReportFilterViewModel filter = null)
        {

            var endDate = filter?.EndDate ?? DateTime.Now;
            var startDate = filter?.StartDate ?? endDate.AddDays(-30);

            var purchases = db.Purchases.Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate).Select(p => new TransactionReportViewModel { Date = p.PurchaseDate, TransactionType = "Purchase", MedicationName = p.Medication.Name, Quantity = p.Quantity, Price = null });
            var sales = db.Sales.Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate).Select(s => new TransactionReportViewModel { Date = s.SaleDate, TransactionType = "Sale", MedicationName = s.Medication.Name, Quantity = s.Quantity, Price = s.Price });
            var transactions = purchases.Union(sales).OrderBy(t => t.Date).ToList();

            using (MemoryStream ms = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4, 25, 25, 25, 25);
                PdfWriter.GetInstance(pdfDoc, ms);
                pdfDoc.Open();

                pdfDoc.Add(new Paragraph("Transaction Report", FontFactory.GetFont("Arial", 16, Font.BOLD)));
                pdfDoc.Add(new Paragraph($"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"));
                pdfDoc.Add(new Paragraph($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}"));
                pdfDoc.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(5);
                table.WidthPercentage = 100;
                table.AddCell("Date");
                table.AddCell("Type");
                table.AddCell("Medication Name");
                table.AddCell("Quantity");
                table.AddCell("Price");

                foreach (var item in transactions)
                {
                    table.AddCell(item.Date.ToString("yyyy-MM-dd HH:mm"));
                    table.AddCell(item.TransactionType);
                    table.AddCell(item.MedicationName);
                    table.AddCell(item.Quantity.ToString());
                    table.AddCell(item.Price.HasValue ? item.Price.Value.ToString("C") : "N/A");
                }

                pdfDoc.Add(table);

                // Add summary totals
                pdfDoc.Add(new Paragraph(" "));
                pdfDoc.Add(new Paragraph($"Total Purchased: {transactions.Where(t => t.TransactionType == "Purchase").Sum(t => t.Quantity)}"));
                pdfDoc.Add(new Paragraph($"Total Sold: {transactions.Where(t => t.TransactionType == "Sale").Sum(t => t.Quantity)}"));
                pdfDoc.Add(new Paragraph($"Total Revenue: {transactions.Where(t => t.TransactionType == "Sale").Sum(t => t.Price ?? 0).ToString("C")}"));

                pdfDoc.Close();

                return File(ms.ToArray(), "application/pdf", "TransactionReport.pdf");
            }
        }

        // New Export to Excel for Transaction Report
        public ActionResult ExportTransactionReportToExcel(TransactionReportFilterViewModel filter = null)
        {
            var endDate = filter?.EndDate ?? DateTime.Now;
            var startDate = filter?.StartDate ?? endDate.AddDays(-30);

            var purchases = db.Purchases.Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate).Select(p => new TransactionReportViewModel { Date = p.PurchaseDate, TransactionType = "Purchase", MedicationName = p.Medication.Name, Quantity = p.Quantity, Price = null });
            var sales = db.Sales.Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate).Select(s => new TransactionReportViewModel { Date = s.SaleDate, TransactionType = "Sale", MedicationName = s.Medication.Name, Quantity = s.Quantity, Price = s.Price });
            var transactions = purchases.Union(sales).OrderBy(t => t.Date).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Transaction Report");
                worksheet.Cells[1, 1].Value = "Transaction Report";
                worksheet.Cells[1, 1, 1, 5].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 1].Value = $"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
                worksheet.Cells[3, 1].Value = $"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}";
                worksheet.Cells[4, 1].LoadFromCollection(transactions, true);

                // Add summary totals
                int row = transactions.Count + 5; // After header + data rows
                worksheet.Cells[row, 1].Value = "Total Purchased";
                worksheet.Cells[row, 2].Value = transactions.Where(t => t.TransactionType == "Purchase").Sum(t => t.Quantity);
                worksheet.Cells[row + 1, 1].Value = "Total Sold";
                worksheet.Cells[row + 1, 2].Value = transactions.Where(t => t.TransactionType == "Sale").Sum(t => t.Quantity);
                worksheet.Cells[row + 2, 1].Value = "Total Revenue";
                worksheet.Cells[row + 2, 2].Value = transactions.Where(t => t.TransactionType == "Sale").Sum(t => t.Price ?? 0);
                worksheet.Cells[row + 2, 2].Style.Numberformat.Format = "$#,##0.00";

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TransactionReport.xlsx");
            }
        }

        // Cleanup
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}