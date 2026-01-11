using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class FrameworksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<FrameworksController> _localizer;

        public FrameworksController(ApplicationDbContext context, IStringLocalizer<FrameworksController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            return LocalRedirect(returnUrl ?? "/");
        }

        // GET: Frameworks
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index(string sortOrder, string searchString, FrameworkFilterViewModel filter)
        {
            ViewData["CurrentSort"] = sortOrder;
            // هنا قمنا بتغيير الفرز الافتراضي ليكون تنازليًا حسب أداء المؤشرات
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["IndicatorsSortParm"] = sortOrder == "indicators" ? "indicators_desc" : "indicators";
            ViewData["DisbursementSortParm"] = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewData["CurrentFilter"] = searchString;

            // Load dropdown/filter data for the ViewModel
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();
            filter.Sectors = await _context.Sectors.ToListAsync();
            filter.IsMinistryUser = false; // Assuming this logic is handled elsewhere

            IQueryable<Framework> frameworksQuery = _context.Frameworks.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                frameworksQuery = frameworksQuery.Where(f => f.Name.Contains(searchString));
            }

            if (filter.SelectedMinistries != null && filter.SelectedMinistries.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Ministries.Any(min => filter.SelectedMinistries.Contains(min.Code))))))));
            }

            if (filter.SelectedDonors != null && filter.SelectedDonors.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Donors.Any(don => filter.SelectedDonors.Contains(don.Code))))))));
            }

            if (filter.SelectedSector != null && filter.SelectedSector.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Sectors.Any(sec => filter.SelectedSector.Contains(sec.Code))))))));
            }

            // Apply sorting logic
            switch (sortOrder)
            {
                case "name_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.Name);
                    break;
                case "indicators":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.IndicatorsPerformance);
                    break;
                case "indicators_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
                case "disbursement":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.DisbursementPerformance);
                    break;
                case "disbursement_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.DisbursementPerformance);
                    break;
                default:
                    // تم تغيير القيمة الافتراضية هنا
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
            }

            filter.Frameworks = await frameworksQuery.ToListAsync();
            return View(filter);
        }

        // POST: Frameworks/CreateInline - AJAX endpoint for inline creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> CreateInline(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = _localizer["Framework name is required."] });
                }

                // Check if framework name already exists
                var existingFramework = await _context.Frameworks.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                if (existingFramework != null)
                {
                    return Json(new { success = false, message = _localizer["A framework with this name already exists."] });
                }

                // Create new framework
                var framework = new Framework
                {
                    Name = name.Trim(),
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Add(framework);
                await _context.SaveChangesAsync();

                // Return the created framework data for frontend update
                return Json(new
                {
                    success = true,
                    framework = new
                    {
                        code = framework.Code,
                        name = framework.Name,
                        indicatorsPerformance = Math.Round(framework.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(framework.DisbursementPerformance, 2)
                    },
                    message = _localizer["Framework created successfully!"]
                });
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new { success = false, message = _localizer["An error occurred while creating the framework. Please try again."] });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            framework.Name = name;
            _context.Update(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Permission(Permissions.DeleteStrategy)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            _context.Frameworks.Remove(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }


        private bool FrameworkExists(int id)
        {
            return _context.Frameworks.Any(e => e.Code == id);
        }

        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult>Monitoring()
        {
            return View(await _context.Frameworks.ToListAsync());
        }

        private async Task RedistributeWeights(int subOutputCode)
        {
            var indicators = await _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .ToListAsync();

            if (indicators.Count == 0)
                return;

            double equalWeight = 100.0 / indicators.Count;

            foreach (var i in indicators)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = indicators.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                indicators.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        // GET: Frameworks/CreateComprehensive
        [Permission(Permissions.AddStrategy)]
        public IActionResult CreateComprehensive()
        {
            return View();
        }

        // POST: Frameworks/CreateComprehensive - Comprehensive framework creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> CreateComprehensive(ComprehensiveFrameworkModel model)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    // Create Framework
                    var framework = new Framework
                    {
                        Name = model.FrameworkName.Trim(),
                        IndicatorsPerformance = 0,
                        DisbursementPerformance = 0,
                        FieldMonitoring = 0,
                        ImpactAssessment = 0
                    };

                    _context.Frameworks.Add(framework);
                    await _context.SaveChangesAsync();

                    // Create Outcomes
                    var outcomeMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.Outcomes.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Outcomes[i].Name))
                        {
                            var outcome = new Outcome
                            {
                                Name = model.Outcomes[i].Name.Trim(),
                                FrameworkCode = framework.Code,
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0
                            };

                            _context.Outcomes.Add(outcome);
                            await _context.SaveChangesAsync();
                            outcomeMapping[i] = outcome.Code;
                        }
                    }

                    // Create Outputs
                    var outputMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.Outputs.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Outputs[i].Name) &&
                            outcomeMapping.ContainsKey(model.Outputs[i].OutcomeIndex))
                        {
                            var output = new Output
                            {
                                Name = model.Outputs[i].Name.Trim(),
                                OutcomeCode = outcomeMapping[model.Outputs[i].OutcomeIndex],
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0
                            };

                            _context.Outputs.Add(output);
                            await _context.SaveChangesAsync();
                            outputMapping[i] = output.Code;
                        }
                    }

                    // Create SubOutputs
                    var subOutputMapping = new Dictionary<int, int>();
                    for (int i = 0; i < model.SubOutputs.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.SubOutputs[i].Name) &&
                            outputMapping.ContainsKey(model.SubOutputs[i].OutputIndex))
                        {
                            var subOutput = new SubOutput
                            {
                                Name = model.SubOutputs[i].Name.Trim(),
                                OutputCode = outputMapping[model.SubOutputs[i].OutputIndex],
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0
                            };

                            _context.SubOutputs.Add(subOutput);
                            await _context.SaveChangesAsync();
                            subOutputMapping[i] = subOutput.Code;
                        }
                    }

                    // Create Indicators
                    for (int i = 0; i < model.Indicators.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Indicators[i].Name) &&
                            subOutputMapping.ContainsKey(model.Indicators[i].SubOutputIndex))
                        {
                            var indicator = new Indicator
                            {
                                Name = model.Indicators[i].Name.Trim(),
                                SubOutputCode = subOutputMapping[model.Indicators[i].SubOutputIndex],
                                Weight = model.Indicators[i].Weight > 0 ? model.Indicators[i].Weight : 100.0,
                                Target = model.Indicators[i].Target,
                                Source = model.Indicators[i].Source?.Trim() ?? string.Empty,
                                IndicatorsPerformance = 0,
                                DisbursementPerformance = 0,
                                FieldMonitoring = 0,
                                ImpactAssessment = 0,
                                IsCommon = false,
                                Active = true,
                                TargetYear = DateTime.Now.AddYears(1),
                                Concept = string.Empty,
                                Description = string.Empty
                            };

                            _context.Indicators.Add(indicator);
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Redistribute weights for each sub-output to ensure they sum to 100%
                    foreach (var subOutputCode in subOutputMapping.Values)
                    {
                        await RedistributeWeights(subOutputCode);
                    }

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = _localizer["Comprehensive framework created successfully!"],
                        frameworkId = framework.Code
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new
                {
                    success = false,
                    message = _localizer["An error occurred while creating the framework. Please try again."]
                });
            }
        }

        // GET: Frameworks/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> ExportExcel(string? searchString)
        {
            var frameworks = await GetFilteredFrameworks(searchString);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Frameworks"].Value);

            // Set RTL for Arabic
            if (isRtl)
            {
                worksheet.RightToLeft = true;
            }

            // Header row
            worksheet.Cell(1, 1).Value = _localizer["Framework Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Disbursement Performance"].Value + " (%)";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int row = 2;
            foreach (var framework in frameworks)
            {
                worksheet.Cell(row, 1).Value = framework.Name;
                worksheet.Cell(row, 2).Value = Math.Round(framework.IndicatorsPerformance, 2);
                worksheet.Cell(row, 3).Value = Math.Round(framework.DisbursementPerformance, 2);
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            var dataRange = worksheet.Range(1, 1, row - 1, 3);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "أطر_العمل" : "Frameworks";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Frameworks/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> ExportPdf(string? searchString)
        {
            var frameworks = await GetFilteredFrameworks(searchString);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

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
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(_localizer["Results Frameworks"].Value)
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Framework Name"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Disbursement Performance"].Value).FontColor(Colors.White).Bold();
                            });

                            // Data rows
                            foreach (var framework in frameworks)
                            {
                                var indicatorsPerformance = Math.Round(framework.IndicatorsPerformance, 2);
                                var disbursementPerformance = Math.Round(framework.DisbursementPerformance, 2);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(framework.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{indicatorsPerformance}%")
                                    .FontColor(GetPerformanceColor(indicatorsPerformance));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{disbursementPerformance}%")
                                    .FontColor(GetPerformanceColor(disbursementPerformance));
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
            var filePrefix = isRtl ? "أطر_العمل" : "Frameworks";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<Framework>> GetFilteredFrameworks(string? searchString)
        {
            IQueryable<Framework> query = _context.Frameworks;

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(f => f.Name.Contains(searchString));
            }

            return await query.OrderByDescending(f => f.IndicatorsPerformance).ToListAsync();
        }

        private static string GetPerformanceColor(double performance)
        {
            return performance switch
            {
                >= 75 => Colors.Green.Darken2,
                >= 50 => Colors.Orange.Darken2,
                _ => Colors.Red.Darken2
            };
        }
    }

    // Models for comprehensive framework creation
    public class ComprehensiveFrameworkModel
    {
        public string FrameworkName { get; set; } = string.Empty;
        public List<OutcomeModel> Outcomes { get; set; } = new List<OutcomeModel>();
        public List<OutputModel> Outputs { get; set; } = new List<OutputModel>();
        public List<SubOutputModel> SubOutputs { get; set; } = new List<SubOutputModel>();
        public List<IndicatorModel> Indicators { get; set; } = new List<IndicatorModel>();
    }

    public class OutcomeModel
    {
        public string Name { get; set; } = string.Empty;
    }

    public class OutputModel
    {
        public string Name { get; set; } = string.Empty;
        public int OutcomeIndex { get; set; }
    }

    public class SubOutputModel
    {
        public string Name { get; set; } = string.Empty;
        public int OutputIndex { get; set; }
    }

    public class IndicatorModel
    {
        public string Name { get; set; } = string.Empty;
        public int SubOutputIndex { get; set; }
        public double Weight { get; set; } = 1.0;
        public int Target { get; set; }
        public string? Source { get; set; }
    }
}
