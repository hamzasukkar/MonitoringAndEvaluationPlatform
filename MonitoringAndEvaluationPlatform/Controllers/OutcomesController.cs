using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Localization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class OutcomesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;
        private readonly IStringLocalizer<OutcomesController> _localizer;

        public OutcomesController(ApplicationDbContext context, IPerformanceService performanceService, IStringLocalizer<OutcomesController> localizer)
        {
            _context = context;
            _performanceService = performanceService;
            _localizer = localizer;
        }

        // GET: Outcomes
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> Index(int? frameworkCode, string sortOrder, string searchString)
        {
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.WeightSortParm = sortOrder == "weight" ? "weight_desc" : "weight";
            ViewBag.IndicatorsSortParm = String.IsNullOrEmpty(sortOrder) ? "indicators" : (sortOrder == "indicators" ? "indicators_desc" : "indicators");
            ViewBag.DisbursementSortParm = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewBag.FrameworkSortParm = sortOrder == "framework" ? "framework_desc" : "framework";
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentFilter = searchString;

            IQueryable<Outcome> outcomesQuery;

            if (frameworkCode == null)
            {
                outcomesQuery = _context.Outcomes
                    .Include(o => o.Framework)
                    .Include(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Project);
            }
            else
            {
                ViewBag.SelectedFrameworkCode = frameworkCode;
                outcomesQuery = _context.Outcomes
                    .Include(o => o.Framework)
                    .Include(o => o.Outputs)
                        .ThenInclude(op => op.SubOutputs)
                            .ThenInclude(so => so.Indicators)
                                .ThenInclude(i => i.Project)
                    .Where(m => m.FrameworkCode == frameworkCode);
            }

            // Apply hierarchical search
            if (!string.IsNullOrEmpty(searchString))
            {
                outcomesQuery = outcomesQuery.Where(o =>
                    EF.Functions.Like(o.Name, $"%{searchString}%") ||
                    o.Outputs.Any(op => EF.Functions.Like(op.Name, $"%{searchString}%")) ||
                    o.Outputs.Any(op => op.SubOutputs.Any(so => EF.Functions.Like(so.Name, $"%{searchString}%"))) ||
                    o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => EF.Functions.Like(i.Name, $"%{searchString}%")))) ||
                    o.Outputs.Any(op => op.SubOutputs.Any(so => so.Indicators.Any(i => i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%"))))
                );
            }

            // Apply sorting
            outcomesQuery = sortOrder switch
            {
                "name" => outcomesQuery.OrderBy(o => o.Name),
                "name_desc" => outcomesQuery.OrderByDescending(o => o.Name),
                "weight" => outcomesQuery.OrderBy(o => o.Weight),
                "weight_desc" => outcomesQuery.OrderByDescending(o => o.Weight),
                "indicators" => outcomesQuery.OrderBy(o => o.IndicatorsPerformance),
                "indicators_desc" => outcomesQuery.OrderByDescending(o => o.IndicatorsPerformance),
                "disbursement" => outcomesQuery.OrderBy(o => o.DisbursementPerformance),
                "disbursement_desc" => outcomesQuery.OrderByDescending(o => o.DisbursementPerformance),
                "framework" => outcomesQuery.OrderBy(o => o.Framework.Name),
                "framework_desc" => outcomesQuery.OrderByDescending(o => o.Framework.Name),
                _ => outcomesQuery.OrderByDescending(o => o.IndicatorsPerformance) // Default: highest Indicators Performance first
            };

            var outcomes = await outcomesQuery.ToListAsync();

            if (outcomes == null)
            {
                return NotFound();
            }

            // Pass framework name to view if viewing specific framework
            if (frameworkCode.HasValue && outcomes.Any())
            {
                ViewBag.FrameworkName = outcomes.First().Framework?.Name;
            }

            // Build search results if searching
            if (!string.IsNullOrEmpty(searchString))
            {
                ViewBag.HasSearchResults = true;
                ViewBag.SearchResults = BuildSearchResults(outcomes, searchString);
            }
            else
            {
                ViewBag.HasSearchResults = false;
            }

            return View(outcomes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddOutcome)]
        public async Task<IActionResult> CreateInline(string name, int frameworkCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Outcome name is required." });
                }

                // Check if outcome name already exists within the same framework
                var existingOutcome = await _context.Outcomes
                    .FirstOrDefaultAsync(o => o.FrameworkCode == frameworkCode &&
                                              o.Name.ToLower() == name.Trim().ToLower());
                if (existingOutcome != null)
                {
                    return Json(new { success = false, message = "An outcome with this name already exists in this framework." });
                }

                var outcome = new Outcome
                {
                    Name = name.Trim(),
                    FrameworkCode = frameworkCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0
                };

                _context.Add(outcome);
                await _context.SaveChangesAsync();

                // Recalculate weights
                await RedistributeWeights(frameworkCode);

                // Reload outcome to get updated weight after redistribution
                await _context.Entry(outcome).ReloadAsync();

                // Fetch all outcomes with updated weights so the client can refresh existing rows
                var allOutcomes = await _context.Outcomes
                    .Where(o => o.FrameworkCode == frameworkCode)
                    .Select(o => new { code = o.Code, weight = Math.Round(o.Weight, 2) })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    outcome = new
                    {
                        code = outcome.Code,
                        name = outcome.Name,
                        weight = Math.Round(outcome.Weight, 2),
                        indicatorsPerformance = Math.Round(outcome.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(outcome.DisbursementPerformance, 2),
                        frameworkName = outcome.Framework?.Name ?? ""
                    },
                    allOutcomes,
                    message = "Outcome created successfully!"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while creating the outcome." });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null) return NotFound();

            outcome.Name = name;
            _context.Update(outcome);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: Outcomes/Delete/5
        [HttpPost]
        [Permission(Permissions.DeleteOutcome)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var outcome = await _context.Outcomes.FindAsync(id);
            if (outcome == null) return NotFound();

            // Store the frameworkCode before deletion for recalculation
            int frameworkCode = outcome.FrameworkCode;

            _context.Outcomes.Remove(outcome);
            await _context.SaveChangesAsync();

            // Redistribute weights among remaining outcomes
            await RedistributeWeights(frameworkCode);

            // Recalculate framework performance (IndicatorsPerformance)
            await _performanceService.UpdateFrameworkPerformance(frameworkCode);

            // Recalculate framework performance (DisbursementPerformance)
            await _performanceService.UpdateFrameworkDisbursementPerformance(frameworkCode);

            return Ok();
        }

        private bool OutcomeExists(int id)
        {
            return _context.Outcomes.Any(e => e.Code == id);
        }

        // GET: FramworkOutcomes
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> FramworkOutcomes(int? id)
        {
            var applicationDbContext = _context.Outcomes.Where(m => m.FrameworkCode == id);
            ViewData["FrameworkName"] = _context.Frameworks.Where(i => i.Code == id).FirstOrDefault().Name;
            return View(await applicationDbContext.ToListAsync());
        }

        private async Task RedistributeWeights(int frameworkCode)
        {
            var outcomes = await _context.Outcomes
                .Where(i => i.FrameworkCode == frameworkCode)
                .ToListAsync();

            if (outcomes.Count == 0)
                return;

            double equalWeight = 100.0 / outcomes.Count;

            foreach (var i in outcomes)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = outcomes.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                outcomes.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> AdjustWeights(int frameworkCode)
        {
            var outcomes = await _context.Outcomes
                .Where(i => i.FrameworkCode == frameworkCode)
                .ToListAsync();

            var model = outcomes.Select(i => new OutcomesViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.FrameworkCode = frameworkCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyOutcome)]
        public async Task<IActionResult> AdjustWeights(List<OutcomesViewModel> model,int frameworkCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.FrameworkCode = frameworkCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var outcome = await _context.Outcomes.FindAsync(vm.Code);
                if (outcome != null)
                {
                    outcome.Weight = vm.Weight;
                    _context.Update(outcome);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateFrameworkPerformance(frameworkCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = frameworkCode });
        }

        // GET: Outcomes/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> ExportExcel(int? frameworkCode)
        {
            var outcomes = await GetFilteredOutcomes(frameworkCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Outcomes"].Value);

            // Set RTL for Arabic
            if (isRtl)
            {
                worksheet.RightToLeft = true;
            }

            // Header row
            worksheet.Cell(1, 1).Value = _localizer["Outcome Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Weight"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 4).Value = _localizer["Disbursement Performance"].Value + " (%)";
            worksheet.Cell(1, 5).Value = _localizer["Framework"].Value;

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int row = 2;
            foreach (var outcome in outcomes)
            {
                worksheet.Cell(row, 1).Value = outcome.Name;
                worksheet.Cell(row, 2).Value = Math.Round(outcome.Weight, 2);
                worksheet.Cell(row, 3).Value = Math.Round(outcome.IndicatorsPerformance, 2);
                worksheet.Cell(row, 4).Value = Math.Round(outcome.DisbursementPerformance, 2);
                worksheet.Cell(row, 5).Value = outcome.Framework?.Name ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            var dataRange = worksheet.Range(1, 1, row - 1, 5);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "النتائج" : "Outcomes";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Outcomes/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadOutcomes)]
        public async Task<IActionResult> ExportPdf(int? frameworkCode)
        {
            var outcomes = await GetFilteredOutcomes(frameworkCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
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
                                col.Item().Text(_localizer["Outcomes"].Value)
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
                                columns.RelativeColumn(3);  // Outcome Name
                                columns.RelativeColumn(1);  // Weight
                                columns.RelativeColumn(2);  // Indicators Performance
                                columns.RelativeColumn(2);  // Disbursement Performance
                                columns.RelativeColumn(2);  // Framework
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Outcome Name"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Weight"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Disbursement Performance"].Value).FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken2).Padding(8)
                                    .Text(_localizer["Framework"].Value).FontColor(Colors.White).Bold();
                            });

                            // Data rows
                            foreach (var outcome in outcomes)
                            {
                                var indicatorsPerformance = Math.Round(outcome.IndicatorsPerformance, 2);
                                var disbursementPerformance = Math.Round(outcome.DisbursementPerformance, 2);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(outcome.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{Math.Round(outcome.Weight, 2)}%");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{indicatorsPerformance}%")
                                    .FontColor(GetPerformanceColor(indicatorsPerformance));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text($"{disbursementPerformance}%")
                                    .FontColor(GetPerformanceColor(disbursementPerformance));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                                    .Text(outcome.Framework?.Name ?? "");
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
            var filePrefix = isRtl ? "النتائج" : "Outcomes";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<Outcome>> GetFilteredOutcomes(int? frameworkCode)
        {
            IQueryable<Outcome> query = _context.Outcomes.Include(o => o.Framework);

            if (frameworkCode.HasValue)
            {
                query = query.Where(o => o.FrameworkCode == frameworkCode.Value);
            }

            return await query.OrderByDescending(o => o.IndicatorsPerformance).ToListAsync();
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

        private List<FrameworkSearchResultViewModel> BuildSearchResults(List<Outcome> outcomes, string searchString)
        {
            var results = new List<FrameworkSearchResultViewModel>();
            var searchTerm = searchString.ToLower();

            foreach (var outcome in outcomes)
            {
                var outcomeResult = new FrameworkSearchResultViewModel
                {
                    Framework = null, // Not used for outcomes
                    Matches = new List<SearchMatch>()
                };

                // Check outcome name
                if (outcome.Name.ToLower().Contains(searchTerm))
                {
                    outcomeResult.Matches.Add(new SearchMatch
                    {
                        Type = "Outcome",
                        Name = outcome.Name,
                        NavigationUrl = Url.Action("Index", "Outputs", new { frameworkCode = outcome.FrameworkCode, outcomeCode = outcome.Code }),
                        Icon = "fas fa-bullseye",
                        ParentPath = outcome.Framework?.Name ?? "",
                        Code = outcome.Code
                    });
                }

                // Check outputs
                foreach (var output in outcome.Outputs ?? Enumerable.Empty<Output>())
                {
                    if (output.Name.ToLower().Contains(searchTerm))
                    {
                        outcomeResult.Matches.Add(new SearchMatch
                        {
                            Type = "Output",
                            Name = output.Name,
                            NavigationUrl = Url.Action("Index", "SubOutputs", new { frameworkCode = outcome.FrameworkCode, outputCode = output.Code }),
                            Icon = "fas fa-cube",
                            ParentPath = $"{outcome.Framework?.Name} > {outcome.Name}",
                            Code = output.Code
                        });
                    }

                    // Check sub-outputs
                    foreach (var subOutput in output.SubOutputs ?? Enumerable.Empty<SubOutput>())
                    {
                        if (subOutput.Name.ToLower().Contains(searchTerm))
                        {
                            outcomeResult.Matches.Add(new SearchMatch
                            {
                                Type = "SubOutput",
                                Name = subOutput.Name,
                                NavigationUrl = Url.Action("Index", "Indicators", new { frameworkCode = outcome.FrameworkCode, subOutputCode = subOutput.Code }),
                                Icon = "fas fa-cubes",
                                ParentPath = $"{outcome.Framework?.Name} > {outcome.Name} > {output.Name}",
                                Code = subOutput.Code
                            });
                        }

                        // Check indicators
                        foreach (var indicator in subOutput.Indicators ?? Enumerable.Empty<Indicator>())
                        {
                            if (indicator.Name.ToLower().Contains(searchTerm))
                            {
                                outcomeResult.Matches.Add(new SearchMatch
                                {
                                    Type = "Indicator",
                                    Name = indicator.Name,
                                    NavigationUrl = Url.Action("Index", "Measures", new { frameworkCode = outcome.FrameworkCode, indicatorCode = indicator.IndicatorCode }),
                                    Icon = "fas fa-chart-line",
                                    ParentPath = $"{outcome.Framework?.Name} > {outcome.Name} > {output.Name} > {subOutput.Name}",
                                    Code = indicator.IndicatorCode
                                });
                            }

                            // Check project
                            if (indicator.Project?.ProjectName != null &&
                                indicator.Project.ProjectName.ToLower().Contains(searchTerm))
                            {
                                outcomeResult.Matches.Add(new SearchMatch
                                {
                                    Type = "Project",
                                    Name = indicator.Project.ProjectName,
                                    NavigationUrl = Url.Action("Details", "Projects", new { id = indicator.Project.ProjectID }),
                                    Icon = "fas fa-project-diagram",
                                    ParentPath = $"{outcome.Framework?.Name} > {outcome.Name} > {output.Name} > {subOutput.Name} > {indicator.Name}",
                                    Code = indicator.Project.ProjectID,
                                    Metadata = new Dictionary<string, object>
                                    {
                                        { "IndicatorName", indicator.Name }
                                    }
                                });
                            }
                        }
                    }
                }

                // Only add to results if there are matches
                if (outcomeResult.Matches.Any())
                {
                    // Use outcome as the grouping key
                    outcomeResult.Framework = new Framework
                    {
                        Code = outcome.Code,
                        Name = outcome.Name
                    };
                    results.Add(outcomeResult);
                }
            }

            return results;
        }
    }
}

