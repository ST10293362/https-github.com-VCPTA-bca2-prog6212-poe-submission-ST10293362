using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PROG6212_POE_R.Models;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PROG6212_POE_R.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IConverter _converter;
        private readonly ICompositeViewEngine _viewEngine; // Add view engine
        private static List<Claim> claims = new List<Claim>();
        private static List<Invoice> invoices = new List<Invoice>();
        private static int claimCounter = 1;
        private static int invoiceCounter = 1;

        public ClaimsController(IConverter converter, ICompositeViewEngine viewEngine) // Inject view engine
        {
            _converter = converter;
            _viewEngine = viewEngine; // Assign view engine
        }

        public IActionResult SubmitClaim()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitClaim(Claim claim, IFormFile file)
        {
            // File size limit (5 MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB
            var allowedFileTypes = new[] { ".pdf", ".docx", ".xlsx" };

            if (file != null && file.Length > 0)
            {
                if (file.Length > maxFileSize)
                {
                    ModelState.AddModelError("File", "File size must be less than 5 MB.");
                    return View(claim);
                }

                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedFileTypes.Contains(fileExtension))
                {
                    ModelState.AddModelError("File", "Invalid file type. Only .pdf, .docx, and .xlsx are allowed.");
                    return View(claim);
                }

                // Save the file to the uploads folder
                var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                var fileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolderPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                claim.SupportingDocument = fileName;
            }

            claim.Id = claimCounter++;
            claim.Status = "Pending"; // Set the initial status of the claim to "Pending" when it is submitted.
            claims.Add(claim); // Store claim in memory
            return RedirectToAction("ClaimStatus");
        }

        public IActionResult ClaimStatus()
        {
            return View(claims);
        }

        public IActionResult ReviewClaims()
        {
            var pendingClaims = claims.Where(c => !c.IsApproved).ToList();
            return View(pendingClaims);
        }

        [HttpPost]
        public IActionResult ApproveClaim(int id)
        {
            var claim = claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claim.IsApproved = true;

                // Create an invoice for the approved claim
                var invoice = new Invoice
                {
                    InvoiceId = invoiceCounter++,
                    LecturerName = claim.LecturerName,
                    Email = claim.Email,
                    TotalAmount = claim.TotalPayment,
                    InvoiceDate = DateTime.Now
                };
                invoices.Add(invoice);
            }
            return RedirectToAction("ReviewClaims");
        }

        public IActionResult ViewInvoices()
        {
            return View(invoices);
        }

        public IActionResult ManageLecturerData()
        {
            return View(claims); 
        }

        [HttpPost]
        public IActionResult UpdateLecturerData(int id, string email)
        {
            var claim = claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claim.Email = email; 
            }
            return RedirectToAction("ManageLecturerData");
        }

        [HttpPost]
        public IActionResult RejectClaim(int id)
        {
            var claim = claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claims.Remove(claim); // Remove the claim if rejected
            }
            return RedirectToAction("ReviewClaims");
        }

        [HttpGet]
        public IActionResult GenerateReport()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Document document = new Document();
                PdfWriter writer = PdfWriter.GetInstance(document, stream);
                document.Open();

                // Add report content here
                document.Add(new Paragraph("Claims Report"));

                document.Close();

                return File(stream.ToArray(), "application/pdf", "ClaimsReport.pdf");
            }
        }

        [HttpGet]
        public IActionResult ExportInvoicesToExcel()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (ExcelPackage package = new ExcelPackage(stream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Invoices");

                    // Add invoice data to the worksheet
                    worksheet.Cells.LoadFromCollection(invoices);

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Invoices.xlsx");
                }
            }
        }

        [HttpGet]
        public IActionResult GeneratePdfInvoice(int id)
        {
            var invoice = invoices.FirstOrDefault(i => i.InvoiceId == id);
            if (invoice != null)
            {
                var htmlContent = RenderViewToString("PdfInvoice", invoice);
                var doc = new HtmlToPdfDocument()
                {
                GlobalSettings = {
                ColorMode = DinkToPdf.ColorMode.Color,
                Orientation = DinkToPdf.Orientation.Portrait,
                PaperSize = DinkToPdf.PaperKind.A4,
            },
                    Objects = {
                new ObjectSettings() {
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" },
                },
            }
                };

                var pdf = _converter.Convert(doc);
                return File(pdf, "application/pdf", "Invoice_" + id + ".pdf");
            }
            return NotFound();
        }

        [HttpGet]
        public IActionResult PdfInvoice(int id)
        {
            var invoice = invoices.FirstOrDefault(i => i.InvoiceId == id);
            return View(invoice);
        }

        private string RenderViewToString(string viewName, object model)
        {
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    sw,
                    new HtmlHelperOptions()
                );
                viewResult.View.RenderAsync(viewContext).Wait();
                return sw.ToString();
            }
        }
    }
}