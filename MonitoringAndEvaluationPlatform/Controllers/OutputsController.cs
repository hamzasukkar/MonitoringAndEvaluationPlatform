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
    public class OutputsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;
        private readonly IStringLocalizer<OutputsController> _localizer;

        public OutputsController(ApplicationDbContext context, IPerformanceService performanceService, IStringLocalizer<OutputsController> localizer)
        {
            _context = context;
            _performanceService = performanceService;
            _localizer = localizer;
        }

        // GET: Outputs
        [Permission(Permissions.ReadOutputs)]
        public async Task<IActionResult> Index(int? frameworkCode, int? outcomeCode, string sortOrder, string searchString)
        {
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.WeightSortParm = sortOrder == "weight" ? "weight_desc" : "weight";
            ViewBag.IndicatorsSortParm = String.IsNullOrEmpty(sortOrder) ? "indicators" : (sortOrder == "indicators" ? "indicators_desc" : "indicators");
            ViewBag.DisbursementSortParm = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewBag.OutcomeSortParm = sortOrder == "outcome" ? "outcome_desc" : "outcome";
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentFilter = searchString;

            // Start with base query including all related entities
            var query = _context.Outputs
                .Include(o => o.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .Include(o => o.SubOutputs)
                    .ThenInclude(so => so.Indicators)
                        .ThenInclude(i => i.Project)
                .AsQueryable();

            // Apply framework filter if frameworkCode is provided
            if (frameworkCode.HasValue)
            {
                query = query.Where(o => o.Outcome.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            // Apply outcome filter if outcomeCode is provided
            if (outcomeCode.HasValue)
            {
                query = query.Where(o => o.OutcomeCode == outcomeCode.Value);
                ViewBag.SelectedOutcomeCode = outcomeCode.Value;
            }

            // Apply hierarchical search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(o =>
                    EF.Functions.Like(o.Name, $"%{searchString}%") ||
                    o.SubOutputs.Any(so => EF.Functions.Like(so.Name, $"%{searchString}%")) ||
                    o.SubOutputs.Any(so => so.Indicators.Any(i => EF.Functions.Like(i.Name, $"%{searchString}%"))) ||
                    o.SubOutputs.Any(so => so.Indicators.Any(i => i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%")))
                );
            }

            // Apply sorting
            query = sortOrder switch
            {
                "name" => query.OrderBy(o => o.Name),
                "name_desc" => query.OrderByDescending(o => o.Name),
                "weight" => query.OrderBy(o => o.Weight),
                "weight_desc" => query.OrderByDescending(o => o.Weight),
                "indicators" => query.OrderBy(o => o.IndicatorsPerformance),
                "indicators_desc" => query.OrderByDescending(o => o.IndicatorsPerformance),
                "disbursement" => query.OrderBy(o => o.DisbursementPerformance),
                "disbursement_desc" => query.OrderByDescending(o => o.DisbursementPerformance),
                "outcome" => query.OrderBy(o => o.Outcome.Name),
                "outcome_desc" => query.OrderByDescending(o => o.Outcome.Name),
                _ => query.OrderByDescending(o => o.IndicatorsPerformance) // Default: highest Indicators Performance first
            };

            // Execute the query
            var outputs = await query.ToListAsync();

            // Set ViewData for breadcrumb
            ViewData["frameworkCode"] = frameworkCode;
            ViewData["outcomeCode"] = outcomeCode;

            // Pass outcome name to view if viewing specific outcome
            if (outcomeCode.HasValue && outputs.Any())
            {
                ViewBag.OutcomeName = outputs.First().Outcome?.Name;
            }

            // Build search results if searching
            if (!string.IsNullOrEmpty(searchString))
            {
                ViewBag.HasSearchResults = true;
                ViewBag.SearchResults = BuildSearchResults(outputs, searchString);
            }
            else
            {
                ViewBag.HasSearchResults = false;
            }

            return View(outputs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddOutput)]
        public async Task<IActionResult> CreateInline(string name, int outcomeCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "Output name is required." });
                }

                // Check if output name already exists within the same outcome
                var existingOutput = await _context.Outputs
                    .FirstOrDefaultAsync(o => o.OutcomeCode == outcomeCode &&
                                              o.Name.ToLower() == name.Trim().ToLower());
                if (existingOutput != null)
                {
                    return Json(new { success = false, message = "An output with this name already exists in this outcome." });
                }

                var output = new Output
                {
                    Name = name.Trim(),
                    OutcomeCode = outcomeCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Add(output);
                await _context.SaveChangesAsync();

                // Recalculate weights
                await RedistributeWeights(outcomeCode);

                return Json(new
                {
                    success = true,
                    output = new
                    {
                        code = output.Code,
                        name = output.Name,
                        weight = Math.Round(output.Weight, 2),
                        indicatorsPerformance = Math.Round(output.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(output.DisbursementPerformance, 2),
                        outcomeName = output.Outcome?.Name ?? ""
                    },
                    message = "Output created successfully!"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while creating the output." });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifyOutput)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var output = await _context.Outputs.FindAsync(id);
            if (output == null) return NotFound();

            output.Name = name;
            _context.Update(output);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Permission(Permissions.DeleteOutput)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var output = await _context.Outputs.FindAsync(id);
            if (output == null) return NotFound();

            // Store the outcomeCode before deletion for recalculation
            int outcomeCode = output.OutcomeCode;

            _context.Outputs.Remove(output);
            await _context.SaveChangesAsync();

            // Redistribute weights among remaining outputs
            await RedistributeWeights(outcomeCode);

            // Recalculate outcome performance (IndicatorsPerformance - cascades up to framework)
            await _performanceService.UpdateOutcomePerformance(outcomeCode);

            // Recalculate outcome performance (DisbursementPerformance, FieldMonitoring, ImpactAssessment - cascades up to framework)
            await _performanceService.UpdateOutcomeDisbursementPerformance(outcomeCode);

            return Ok();
        }

        private bool OutputExists(int id)
        {
            return _context.Outputs.Any(e => e.Code == id);
        }

        private async Task RedistributeWeights(int outcomeCode)
        {
            var outputs = await _context.Outputs
                .Where(i => i.OutcomeCode == outcomeCode)
                .ToListAsync();

            if (outputs.Count == 0)
                return;

            double equalWeight = 100.0 / outputs.Count;

            foreach (var i in outputs)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = outputs.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                outputs.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        // GET: Indicators/AdjustWeights/5
        [Permission(Permissions.ModifyOutput)]
        public async Task<IActionResult> AdjustWeights(int outcomeCode)
        {
            var outputs = await _context.Outputs
                .Where(i => i.OutcomeCode == outcomeCode)
                .ToListAsync();

            var model = outputs.Select(i => new OutputViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.OutcomeCode = outcomeCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyOutput)]
        public async Task<IActionResult> AdjustWeights(List<OutputViewModel> model, int outcomeCode)
        {
            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.OutcomeCode = outcomeCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var output = await _context.Outputs.FindAsync(vm.Code);
                if (output != null)
                {
                    output.Weight = vm.Weight;
                    _context.Update(output);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateOutcomePerformance(outcomeCode);

            return RedirectToAction(nameof(Index), new { outcomeCode = outcomeCode });
        }

        // GET: Outputs/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadOutputs)]
        public async Task<IActionResult> ExportExcel(int? frameworkCode, int? outcomeCode)
        {
            var outputs = await GetFilteredOutputs(frameworkCode, outcomeCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Outputs"].Value);

            if (isRtl) worksheet.RightToLeft = true;

            worksheet.Cell(1, 1).Value = _localizer["Output Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Weight"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 4).Value = _localizer["Disbursement Performance"].Value + " (%)";
            worksheet.Cell(1, 5).Value = _localizer["Outcome"].Value;

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var output in outputs)
            {
                worksheet.Cell(row, 1).Value = output.Name;
                worksheet.Cell(row, 2).Value = Math.Round(output.Weight, 2);
                worksheet.Cell(row, 3).Value = Math.Round(output.IndicatorsPerformance, 2);
                worksheet.Cell(row, 4).Value = Math.Round(output.DisbursementPerformance, 2);
                worksheet.Cell(row, 5).Value = output.Outcome?.Name ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            var dataRange = worksheet.Range(1, 1, row - 1, 5);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "البرامج" : "Outputs";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Outputs/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadOutputs)]
        public async Task<IActionResult> ExportPdf(int? frameworkCode, int? outcomeCode)
        {
            var outputs = await GetFilteredOutputs(frameworkCode, outcomeCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    if (isRtl) page.ContentFromRightToLeft();

                    page.Header().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Medium)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(_localizer["Outputs"].Value).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Output Name"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Weight"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Disbursement Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Outcome"].Value).FontColor(Colors.White).Bold();
                        });

                        foreach (var output in outputs)
                        {
                            var indicatorsPerf = Math.Round(output.IndicatorsPerformance, 2);
                            var disbursementPerf = Math.Round(output.DisbursementPerformance, 2);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(output.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{Math.Round(output.Weight, 2)}%");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{indicatorsPerf}%").FontColor(GetPerformanceColor(indicatorsPerf));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{disbursementPerf}%").FontColor(GetPerformanceColor(disbursementPerf));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(output.Outcome?.Name ?? "");
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(_localizer["Page"].Value + " ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            var filePrefix = isRtl ? "البرامج" : "Outputs";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<Output>> GetFilteredOutputs(int? frameworkCode, int? outcomeCode)
        {
            var query = _context.Outputs.Include(o => o.Outcome).AsQueryable();

            if (frameworkCode.HasValue)
                query = query.Where(o => o.Outcome.FrameworkCode == frameworkCode.Value);

            if (outcomeCode.HasValue)
                query = query.Where(o => o.OutcomeCode == outcomeCode.Value);

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

        private List<FrameworkSearchResultViewModel> BuildSearchResults(List<Output> outputs, string searchString)
        {
            var results = new List<FrameworkSearchResultViewModel>();
            var searchTerm = searchString.ToLower();

            foreach (var output in outputs)
            {
                var outputResult = new FrameworkSearchResultViewModel
                {
                    Framework = null, // Not used for outputs
                    Matches = new List<SearchMatch>()
                };

                // Check output name
                if (output.Name.ToLower().Contains(searchTerm))
                {
                    outputResult.Matches.Add(new SearchMatch
                    {
                        Type = "Output",
                        Name = output.Name,
                        NavigationUrl = Url.Action("Index", "SubOutputs", new { frameworkCode = output.Outcome?.FrameworkCode, outputCode = output.Code }),
                        Icon = "fas fa-cube",
                        ParentPath = $"{output.Outcome?.Framework?.Name} > {output.Outcome?.Name}",
                        Code = output.Code
                    });
                }

                // Check sub-outputs
                foreach (var subOutput in output.SubOutputs ?? Enumerable.Empty<SubOutput>())
                {
                    if (subOutput.Name.ToLower().Contains(searchTerm))
                    {
                        outputResult.Matches.Add(new SearchMatch
                        {
                            Type = "SubOutput",
                            Name = subOutput.Name,
                            NavigationUrl = Url.Action("Index", "Indicators", new { frameworkCode = output.Outcome?.FrameworkCode, subOutputCode = subOutput.Code }),
                            Icon = "fas fa-cubes",
                            ParentPath = $"{output.Outcome?.Framework?.Name} > {output.Outcome?.Name} > {output.Name}",
                            Code = subOutput.Code
                        });
                    }

                    // Check indicators
                    foreach (var indicator in subOutput.Indicators ?? Enumerable.Empty<Indicator>())
                    {
                        if (indicator.Name.ToLower().Contains(searchTerm))
                        {
                            outputResult.Matches.Add(new SearchMatch
                            {
                                Type = "Indicator",
                                Name = indicator.Name,
                                NavigationUrl = Url.Action("Index", "Measures", new { frameworkCode = output.Outcome?.FrameworkCode, indicatorCode = indicator.IndicatorCode }),
                                Icon = "fas fa-chart-line",
                                ParentPath = $"{output.Outcome?.Framework?.Name} > {output.Outcome?.Name} > {output.Name} > {subOutput.Name}",
                                Code = indicator.IndicatorCode
                            });
                        }

                        // Check project
                        if (indicator.Project?.ProjectName != null &&
                            indicator.Project.ProjectName.ToLower().Contains(searchTerm))
                        {
                            outputResult.Matches.Add(new SearchMatch
                            {
                                Type = "Project",
                                Name = indicator.Project.ProjectName,
                                NavigationUrl = Url.Action("Details", "Projects", new { id = indicator.Project.ProjectID }),
                                Icon = "fas fa-project-diagram",
                                ParentPath = $"{output.Outcome?.Framework?.Name} > {output.Outcome?.Name} > {output.Name} > {subOutput.Name} > {indicator.Name}",
                                Code = indicator.Project.ProjectID,
                                Metadata = new Dictionary<string, object>
                                {
                                    { "IndicatorName", indicator.Name }
                                }
                            });
                        }
                    }
                }

                // Only add to results if there are matches
                if (outputResult.Matches.Any())
                {
                    // Use output as the grouping key
                    outputResult.Framework = new Framework
                    {
                        Code = output.Code,
                        Name = output.Name
                    };
                    results.Add(outputResult);
                }
            }

            return results;
        }
    }
}

