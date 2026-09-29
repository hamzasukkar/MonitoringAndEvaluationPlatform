using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = "SystemAdministrator")]
    public class ProjectManagersController : Controller
    {
        private const long MaxImportFileBytes = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _context;

        private readonly ICurrencyConversionService _currencyConversion;
        private readonly IStringLocalizer<ProjectManagersController> _localizer;
        private readonly ILogger<ProjectManagersController> _logger;

        public ProjectManagersController(
            ApplicationDbContext context,
            ICurrencyConversionService currencyConversion,
            IStringLocalizer<ProjectManagersController> localizer,
            ILogger<ProjectManagersController> logger)
        {
            _currencyConversion = currencyConversion;
            _context = context;
            _localizer = localizer;
            _logger = logger;
        }

        // GET: ProjectManagers
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProjectManagers.ToListAsync());
        }

        // GET: ProjectManagers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projectManager = await _context.ProjectManagers
                .FirstOrDefaultAsync(m => m.Code == id);
            if (projectManager == null)
            {
                return NotFound();
            }

            // Get associated projects
            var projects = await _context.Projects
                .Include(p => p.Sector)
                .Include(p => p.Donors)
                .Include(p => p.SuperVisor)
                .Include(p => p.Ministries)
                .Include(p => p.Governorates)
                .Where(p => p.ProjectManagerCode == id)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = (await _currencyConversion.GetConverterAsync()).SumBudget(projects);
            ViewBag.Projects = projects;

            return View(projectManager);
        }

        private bool ProjectManagerExists(int id)
        {
            return _context.ProjectManagers.Any(e => e.Code == id);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string Name, string PhoneNumber, string Email)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return Json(new { success = false, message = "Name is required." });
            }

            var projectManager = new ProjectManager
            {
                Name = Name,
                PhoneNumber = PhoneNumber,
                Email = Email
            };

            try
            {
                _context.ProjectManagers.Add(projectManager);
                await _context.SaveChangesAsync();
                return Json(new { success = true, projectManager = new { projectManager.Code, projectManager.Name, projectManager.PhoneNumber, projectManager.Email } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating project manager: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var projectManager = await _context.ProjectManagers.FindAsync(id);
            if (projectManager == null)
                return Json(new { success = false, message = "Project Manager not found" });

            switch (field.ToLower())
            {
                case "name":
                    projectManager.Name = value;
                    break;
                case "phonenumber":
                    projectManager.PhoneNumber = value;
                    break;
                case "email":
                    projectManager.Email = value;
                    break;
                default:
                    return Json(new { success = false, message = "Invalid field" });
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var projectManager = await _context.ProjectManagers.FindAsync(id);
            if (projectManager == null)
                return Json(new { success = false, message = "Project Manager not found" });

            var projectCount = await _context.Projects.CountAsync(p => p.ProjectManagerCode == id);
            if (projectCount > 0)
                return Json(new { success = false, message = _localizer["This project manager is assigned to {0} project(s). Reassign them to another project manager before deleting.", projectCount].Value });

            try
            {
                _context.ProjectManagers.Remove(projectManager);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickUpdate(int id, string name)
        {
            var projectManager = await _context.ProjectManagers.FindAsync(id);
            if (projectManager == null)
                return Json(new { success = false, message = "Project Manager not found" });

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required" });

            projectManager.Name = name;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: ProjectManagers/ExportExcel
        // The sheet layout (Code | Name | Phone Number | Email) is also the ImportExcel format.
        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var projectManagers = await _context.ProjectManagers.OrderBy(p => p.Code).ToListAsync();
            var isRtl = IsArabicCulture();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Project Managers"].Value);

            // Set RTL for Arabic
            if (isRtl)
            {
                worksheet.RightToLeft = true;
            }

            // Header row
            worksheet.Cell(1, 1).Value = _localizer["Code"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Project Manager Name"].Value;
            worksheet.Cell(1, 3).Value = _localizer["Phone Number"].Value;
            worksheet.Cell(1, 4).Value = _localizer["Email"].Value;

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Keep phone numbers as text so Excel does not drop leading zeros on rows typed in later
            worksheet.Column(3).Style.NumberFormat.Format = "@";

            // Data rows
            int row = 2;
            foreach (var projectManager in projectManagers)
            {
                worksheet.Cell(row, 1).Value = projectManager.Code;
                worksheet.Cell(row, 2).Value = projectManager.Name ?? string.Empty;
                worksheet.Cell(row, 3).Value = projectManager.PhoneNumber ?? string.Empty;
                worksheet.Cell(row, 4).Value = projectManager.Email ?? string.Empty;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            var dataRange = worksheet.Range(1, 1, row - 1, 4);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var filePrefix = isRtl ? "مدراء_المشاريع" : "ProjectManagers";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: ProjectManagers/ExportPdf
        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            var projectManagers = await _context.ProjectManagers.OrderBy(p => p.Name).ToListAsync();
            var isRtl = IsArabicCulture();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    if (isRtl)
                    {
                        page.ContentFromRightToLeft();
                    }

                    page.Header()
                        .PaddingBottom(10)
                        .BorderBottom(1)
                        .BorderColor(Colors.Grey.Medium)
                        .Column(col =>
                        {
                            col.Item().Text(_localizer["Project Managers List"].Value)
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);
                            col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Project Manager Name"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Phone Number"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Email"].Value).FontColor(Colors.White).Bold();
                            });

                            // Data rows
                            foreach (var projectManager in projectManagers)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(projectManager.Name ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(projectManager.PhoneNumber ?? string.Empty);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(projectManager.Email ?? string.Empty);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span(_localizer["Page"].Value + " ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var filePrefix = isRtl ? "مدراء_المشاريع" : "ProjectManagers";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // POST: ProjectManagers/ImportExcel
        // Reads the ExportExcel layout by column position (so Arabic and English exports both work).
        // A row whose Code exists updates that project manager; a row with an empty or unknown Code is added.
        // All rows are validated first and nothing is saved if any row has an error.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = _localizer["Please select a file to upload."].Value });
            }

            if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = _localizer["Only .xlsx files are supported."].Value });
            }

            if (file.Length > MaxImportFileBytes)
            {
                return Json(new { success = false, message = _localizer["The file is too large (maximum 5 MB)."].Value });
            }

            var rows = new List<(int RowNumber, string Code, string Name, string PhoneNumber, string Email)>();
            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                // Row 1 is the header
                for (int r = 2; r <= lastRow; r++)
                {
                    var code = worksheet.Cell(r, 1).GetString().Trim();
                    var name = worksheet.Cell(r, 2).GetString().Trim();
                    var phoneNumber = worksheet.Cell(r, 3).GetString().Trim();
                    var email = worksheet.Cell(r, 4).GetString().Trim();

                    if (code.Length == 0 && name.Length == 0 && phoneNumber.Length == 0 && email.Length == 0)
                    {
                        continue;
                    }

                    rows.Add((r, code, name, phoneNumber, email));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Project manager import could not read {FileName}", file.FileName);
                return Json(new { success = false, message = _localizer["The file could not be read as an Excel workbook."].Value });
            }

            if (rows.Count == 0)
            {
                return Json(new { success = false, message = _localizer["The file contains no data rows."].Value });
            }

            var existing = await _context.ProjectManagers.ToDictionaryAsync(p => p.Code);
            var seenCodes = new HashSet<int>();
            var errors = new List<string>();
            var validRows = new List<(int? Code, string Name, string PhoneNumber, string Email)>();

            foreach (var row in rows)
            {
                if (row.Name.Length == 0)
                {
                    errors.Add(_localizer["Row {0}: Name is required.", row.RowNumber].Value);
                }

                int? code = null;
                if (row.Code.Length > 0)
                {
                    if (!int.TryParse(row.Code, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCode))
                    {
                        errors.Add(_localizer["Row {0}: Code '{1}' is not a number.", row.RowNumber, row.Code].Value);
                    }
                    else if (!seenCodes.Add(parsedCode))
                    {
                        errors.Add(_localizer["Row {0}: Code {1} appears more than once in the file.", row.RowNumber, parsedCode].Value);
                    }
                    else if (existing.ContainsKey(parsedCode))
                    {
                        code = parsedCode;
                    }
                    // Otherwise the Code no longer exists (e.g. deleted after the export): leave code null so
                    // the row is added back as a new project manager with a new Code.
                }

                validRows.Add((code, row.Name, row.PhoneNumber, row.Email));
            }

            if (errors.Count > 0)
            {
                return Json(new { success = false, message = _localizer["The file has errors. Nothing was imported."].Value, errors });
            }

            int added = 0, updated = 0, unchanged = 0;
            foreach (var row in validRows)
            {
                if (row.Code is int code)
                {
                    // Cells are trimmed on read, so compare trimmed values; otherwise re-importing an
                    // untouched export would rewrite every row whose stored value has stray spaces.
                    var projectManager = existing[code];
                    if ((projectManager.Name ?? string.Empty).Trim() == row.Name
                        && (projectManager.PhoneNumber ?? string.Empty).Trim() == row.PhoneNumber
                        && (projectManager.Email ?? string.Empty).Trim() == row.Email)
                    {
                        unchanged++;
                        continue;
                    }

                    projectManager.Name = row.Name;
                    projectManager.PhoneNumber = row.PhoneNumber;
                    projectManager.Email = row.Email;
                    updated++;
                }
                else
                {
                    _context.ProjectManagers.Add(new ProjectManager
                    {
                        Name = row.Name,
                        PhoneNumber = row.PhoneNumber,
                        Email = row.Email
                    });
                    added++;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Project manager import failed to save");
                return Json(new { success = false, message = _localizer["An error occurred while saving the imported data."].Value });
            }

            return Json(new
            {
                success = true,
                added,
                updated,
                unchanged,
                message = _localizer["Import completed: {0} added, {1} updated, {2} unchanged.", added, updated, unchanged].Value
            });
        }

        private bool IsArabicCulture()
        {
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            return culture.StartsWith("ar");
        }
    }
}
