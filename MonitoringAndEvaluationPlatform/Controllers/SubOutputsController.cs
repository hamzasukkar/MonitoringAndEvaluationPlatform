using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class SubOutputsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPerformanceService _performanceService;
        private readonly IStringLocalizer<SubOutputsController> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubOutputsController(
            ApplicationDbContext context,
            IPerformanceService performanceService,
            IStringLocalizer<SubOutputsController> localizer,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _performanceService = performanceService;
            _localizer = localizer;
            _userManager = userManager;
        }

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        // GET: SubOutputs
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> Index(int? frameworkCode, int? outputCode, string sortOrder, string searchString)
        {
            ViewBag.NameSortParm = sortOrder == "name" ? "name_desc" : "name";
            ViewBag.WeightSortParm = sortOrder == "weight" ? "weight_desc" : "weight";
            ViewBag.IndicatorsSortParm = String.IsNullOrEmpty(sortOrder) ? "indicators" : (sortOrder == "indicators" ? "indicators_desc" : "indicators");
            ViewBag.DisbursementSortParm = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewBag.OutputSortParm = sortOrder == "output" ? "output_desc" : "output";
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentFilter = searchString;

            IQueryable<SubOutput> query = _context.SubOutputs
                .Include(s => s.Output)
                    .ThenInclude(o => o.Outcome)
                        .ThenInclude(oc => oc.Framework)
                .Include(s => s.Indicators)
                    .ThenInclude(i => i.Project);

            if (frameworkCode != null)
            {
                // Filter by frameworkCode
                query = query.Where(s => s.Output.Outcome.FrameworkCode == frameworkCode);
                ViewBag.SelectedFrameworkCode = frameworkCode; // Store for view
            }
            else if (outputCode != null)
            {
                // Filter by outputCode
                query = query.Where(s => s.OutputCode == outputCode);
                ViewBag.SelectedOutputCode = outputCode; // Store for view
            }
            // If both are null, we'll return all records

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            // Apply hierarchical search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s =>
                    EF.Functions.Like(s.Name, $"%{searchString}%") ||
                    s.Indicators.Any(i => EF.Functions.Like(i.Name, $"%{searchString}%")) ||
                    s.Indicators.Any(i => i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%"))
                );
            }

            // Apply sorting
            query = sortOrder switch
            {
                "name" => query.OrderBy(s => s.Name),
                "name_desc" => query.OrderByDescending(s => s.Name),
                "weight" => query.OrderBy(s => s.Weight),
                "weight_desc" => query.OrderByDescending(s => s.Weight),
                "indicators" => query.OrderBy(s => s.IndicatorsPerformance),
                "indicators_desc" => query.OrderByDescending(s => s.IndicatorsPerformance),
                "disbursement" => query.OrderBy(s => s.DisbursementPerformance),
                "disbursement_desc" => query.OrderByDescending(s => s.DisbursementPerformance),
                "output" => query.OrderBy(s => s.Output.Name),
                "output_desc" => query.OrderByDescending(s => s.Output.Name),
                _ => query.OrderByDescending(s => s.IndicatorsPerformance) // Default: highest Indicators Performance first
            };

            var subOutputs = await query.ToListAsync();

            if (subOutputs == null)
            {
                return NotFound();
            }

            // Set ViewData for breadcrumb
            ViewData["frameworkCode"] = frameworkCode;
            ViewData["outputCode"] = outputCode;

            // Pass output name to view if viewing specific output
            if (outputCode.HasValue && subOutputs.Any())
            {
                ViewBag.OutputName = subOutputs.First().Output?.Name;
            }

            // Build search results if searching
            if (!string.IsNullOrEmpty(searchString))
            {
                ViewBag.HasSearchResults = true;
                ViewBag.SearchResults = BuildSearchResults(subOutputs, searchString);
            }
            else
            {
                ViewBag.HasSearchResults = false;
            }

            return View(subOutputs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddSubOutput)]
        public async Task<IActionResult> CreateInline(string name, int outputCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = "SubOutput name is required." });
                }

                var outputWithFramework = await _context.Outputs
                    .Include(o => o.Outcome).ThenInclude(oc => oc.Framework)
                    .FirstOrDefaultAsync(o => o.Code == outputCode);
                if (outputWithFramework == null)
                {
                    return Json(new { success = false, message = "Output not found." });
                }

                var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
                if (!isAdmin && outputWithFramework.Outcome?.Framework?.MinistryCode != scopedMinistryCode)
                {
                    return Json(new { success = false, message = "You are not authorized to modify this output." });
                }

                // Check if suboutput name already exists within the same output
                var existingSubOutput = await _context.SubOutputs
                    .FirstOrDefaultAsync(s => s.OutputCode == outputCode &&
                                              s.Name.ToLower() == name.Trim().ToLower());
                if (existingSubOutput != null)
                {
                    return Json(new { success = false, message = "A suboutput with this name already exists in this output." });
                }

                var subOutput = new SubOutput
                {
                    Name = name.Trim(),
                    OutputCode = outputCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0
                };

                _context.Add(subOutput);
                await _context.SaveChangesAsync();

                // Recalculate weights
                await RedistributeWeights(outputCode);

                // Reload subOutput to get updated weight after redistribution
                await _context.Entry(subOutput).ReloadAsync();

                // Fetch all subOutputs with updated weights so the client can refresh existing rows
                var allSubOutputs = await _context.SubOutputs
                    .Where(s => s.OutputCode == outputCode)
                    .Select(s => new { code = s.Code, weight = Math.Round(s.Weight, 2) })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    subOutput = new
                    {
                        code = subOutput.Code,
                        name = subOutput.Name,
                        weight = Math.Round(subOutput.Weight, 2),
                        indicatorsPerformance = Math.Round(subOutput.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(subOutput.DisbursementPerformance, 2),
                        outputName = subOutput.Output?.Name ?? ""
                    },
                    allSubOutputs,
                    message = "SubOutput created successfully!"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while creating the suboutput." });
            }
        }

        [HttpPost]
        [Permission(Permissions.ModifySubOutput)]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var subOutput = await _context.SubOutputs
                .Include(s => s.Output).ThenInclude(o => o.Outcome).ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(s => s.Code == id);

            if (subOutput == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && subOutput.Output?.Outcome?.Framework?.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            subOutput.Name = name;
            _context.Update(subOutput);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: SubOutputs/Delete/5
        [HttpPost]
        [Permission(Permissions.DeleteSubOutput)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subOutput = await _context.SubOutputs
                .Include(s => s.Output).ThenInclude(o => o.Outcome).ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(s => s.Code == id);

            if (subOutput == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && subOutput.Output?.Outcome?.Framework?.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            // Store the outputCode before deletion for recalculation
            int outputCode = subOutput.OutputCode;

            _context.SubOutputs.Remove(subOutput);
            await _context.SaveChangesAsync();

            // Redistribute weights among remaining subOutputs
            await RedistributeWeights(outputCode);

            // Recalculate output performance (IndicatorsPerformance - cascades up to outcome and framework)
            await _performanceService.UpdateOutputPerformance(outputCode);

            // Recalculate output performance (DisbursementPerformance - cascades up to outcome and framework)
            await _performanceService.UpdateOutputDisbursementPerformance(outputCode);

            return Ok();
        }

        private bool SubOutputExists(int id)
        {
            return _context.SubOutputs.Any(e => e.Code == id);
        }


        private async Task RedistributeWeights(int outputCode)
        {
            var subOutputs = await _context.SubOutputs
                .Where(i => i.OutputCode == outputCode)
                .ToListAsync();

            if (subOutputs.Count == 0)
                return;

            double equalWeight = 100.0 / subOutputs.Count;

            foreach (var i in subOutputs)
            {
                i.Weight = Math.Round(equalWeight, 2);
                _context.Entry(i).State = EntityState.Modified;
            }

            // Adjust the last one so the sum is exactly 100
            double total = subOutputs.Sum(i => i.Weight);
            if (Math.Abs(total - 100.0) > 0.01)
            {
                double correction = 100.0 - total;
                subOutputs.Last().Weight += correction;
            }

            await _context.SaveChangesAsync();
        }

        // GET: Indicators/AdjustWeights/5
        [Permission(Permissions.ModifySubOutput)]
        public async Task<IActionResult> AdjustWeights(int outputCode)
        {
            var output = await _context.Outputs
                .Include(o => o.Outcome).ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(o => o.Code == outputCode);
            if (output == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && output.Outcome?.Framework?.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            var subOutputs = await _context.SubOutputs
                .Where(i => i.OutputCode == outputCode)
                .ToListAsync();

            var model = subOutputs.Select(i => new SubOutputViewModel
            {
                Code = i.Code,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.OutputCode = outputCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifySubOutput)]
        public async Task<IActionResult> AdjustWeights(List<SubOutputViewModel> model, int outputCode)
        {
            var output = await _context.Outputs
                .Include(o => o.Outcome).ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(o => o.Code == outputCode);
            if (output == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && output.Outcome?.Framework?.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", "Total weight must equal 100%.");
                ViewBag.OutputCode = outputCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var subOutput = await _context.SubOutputs.FindAsync(vm.Code);
                if (subOutput != null)
                {
                    subOutput.Weight = vm.Weight;
                    _context.Update(subOutput);
                }
            }

            await _context.SaveChangesAsync();

            await _performanceService.UpdateOutputPerformance(outputCode);


            return RedirectToAction(nameof(Index), new { outputCode = outputCode });
        }

        // GET: SubOutputs/ProjectsList
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> ProjectsList(int? frameworkCode, int? outputCode, string searchString)
        {
            IQueryable<SubOutput> query = _context.SubOutputs
                .Include(s => s.Output)
                .Include(s => s.Indicators)
                    .ThenInclude(i => i.Project)
                .Include(s => s.Output.Outcome.Framework);

            if (frameworkCode != null)
            {
                // Filter by frameworkCode
                query = query.Where(s => s.Output.Outcome.FrameworkCode == frameworkCode);
                ViewBag.SelectedFrameworkCode = frameworkCode; // Store for view
            }
            else if (outputCode != null)
            {
                // Filter by outputCode
                query = query.Where(s => s.OutputCode == outputCode);
                ViewBag.SelectedOutputCode = outputCode; // Store for view
            }
            // If both are null, we'll return all records

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => EF.Functions.Like(s.Name, $"%{searchString}%") || EF.Functions.Like(s.Output.Name, $"%{searchString}%"));
                ViewBag.SearchString = searchString;
            }

            var subOutputs = await query
                .OrderByDescending(s => s.IndicatorsPerformance)
                .ToListAsync();

            if (subOutputs == null)
            {
                return NotFound();
            }

            // Set ViewData for breadcrumb
            ViewData["frameworkCode"] = frameworkCode;
            ViewData["outputCode"] = outputCode;

            // Load frameworks for filter dropdown
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            return View(subOutputs);
        }

        // GET: SubOutputs/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> ExportExcel(int? frameworkCode, int? outputCode)
        {
            var subOutputs = await GetFilteredSubOutputs(frameworkCode, outputCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["SubOutputs"].Value);

            if (isRtl) worksheet.RightToLeft = true;

            worksheet.Cell(1, 1).Value = _localizer["SubOutput Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Weight"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 4).Value = _localizer["Disbursement Performance"].Value + " (%)";
            worksheet.Cell(1, 5).Value = _localizer["Output"].Value;

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var subOutput in subOutputs)
            {
                worksheet.Cell(row, 1).Value = subOutput.Name;
                worksheet.Cell(row, 2).Value = Math.Round(subOutput.Weight, 2);
                worksheet.Cell(row, 3).Value = Math.Round(subOutput.IndicatorsPerformance, 2);
                worksheet.Cell(row, 4).Value = Math.Round(subOutput.DisbursementPerformance, 2);
                worksheet.Cell(row, 5).Value = subOutput.Output?.Name ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            var dataRange = worksheet.Range(1, 1, row - 1, 5);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "البرامج_الفرعية" : "SubOutputs";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: SubOutputs/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> ExportPdf(int? frameworkCode, int? outputCode)
        {
            var subOutputs = await GetFilteredSubOutputs(frameworkCode, outputCode);
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
                                col.Item().Text(_localizer["SubOutputs"].Value).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
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
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["SubOutput Name"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Weight"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Disbursement Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Output"].Value).FontColor(Colors.White).Bold();
                        });

                        foreach (var subOutput in subOutputs)
                        {
                            var indicatorsPerf = Math.Round(subOutput.IndicatorsPerformance, 2);
                            var disbursementPerf = Math.Round(subOutput.DisbursementPerformance, 2);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(subOutput.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{Math.Round(subOutput.Weight, 2)}%");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{indicatorsPerf}%").FontColor(GetPerformanceColor(indicatorsPerf));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{disbursementPerf}%").FontColor(GetPerformanceColor(disbursementPerf));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(subOutput.Output?.Name ?? "");
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
            var filePrefix = isRtl ? "البرامج_الفرعية" : "SubOutputs";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<SubOutput>> GetFilteredSubOutputs(int? frameworkCode, int? outputCode)
        {
            var query = _context.SubOutputs
                .Include(s => s.Output)
                    .ThenInclude(o => o.Outcome)
                        .ThenInclude(oc => oc.Framework)
                .AsQueryable();

            if (frameworkCode.HasValue)
                query = query.Where(s => s.Output.Outcome.FrameworkCode == frameworkCode.Value);

            if (outputCode.HasValue)
                query = query.Where(s => s.OutputCode == outputCode.Value);

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            return await query.OrderByDescending(s => s.IndicatorsPerformance).ToListAsync();
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

        // GET: SubOutputs/ExportProjectsListExcel
        [HttpGet]
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> ExportProjectsListExcel(int? frameworkCode, int? outputCode)
        {
            var subOutputs = await GetFilteredSubOutputsWithOutput(frameworkCode, outputCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["SubOutputs Projects"].Value);

            if (isRtl) worksheet.RightToLeft = true;

            worksheet.Cell(1, 1).Value = _localizer["SubOutput Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Disbursement Performance"].Value + " (%)";
            worksheet.Cell(1, 4).Value = _localizer["Output"].Value;

            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var subOutput in subOutputs)
            {
                worksheet.Cell(row, 1).Value = subOutput.Name;
                worksheet.Cell(row, 2).Value = Math.Round(subOutput.IndicatorsPerformance, 2);
                worksheet.Cell(row, 3).Value = Math.Round(subOutput.DisbursementPerformance, 2);
                worksheet.Cell(row, 4).Value = subOutput.Output?.Name ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            var dataRange = worksheet.Range(1, 1, row - 1, 4);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "البرامج_الفرعية_المشاريع" : "SubOutputs_Projects";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: SubOutputs/ExportProjectsListPdf
        [HttpGet]
        [Permission(Permissions.ReadSubOutputs)]
        public async Task<IActionResult> ExportProjectsListPdf(int? frameworkCode, int? outputCode)
        {
            var subOutputs = await GetFilteredSubOutputsWithOutput(frameworkCode, outputCode);
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
                                col.Item().Text(_localizer["SubOutputs Projects"].Value).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["SubOutput Name"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Disbursement Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Output"].Value).FontColor(Colors.White).Bold();
                        });

                        foreach (var subOutput in subOutputs)
                        {
                            var indicatorsPerf = Math.Round(subOutput.IndicatorsPerformance, 2);
                            var disbursementPerf = Math.Round(subOutput.DisbursementPerformance, 2);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(subOutput.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{indicatorsPerf}%").FontColor(GetPerformanceColor(indicatorsPerf));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{disbursementPerf}%").FontColor(GetPerformanceColor(disbursementPerf));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(subOutput.Output?.Name ?? "");
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
            var filePrefix = isRtl ? "البرامج_الفرعية_المشاريع" : "SubOutputs_Projects";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<SubOutput>> GetFilteredSubOutputsWithOutput(int? frameworkCode, int? outputCode)
        {
            var query = _context.SubOutputs
                .Include(s => s.Output)
                .Include(s => s.Output.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .AsQueryable();

            if (frameworkCode.HasValue)
                query = query.Where(s => s.Output.Outcome.FrameworkCode == frameworkCode.Value);

            if (outputCode.HasValue)
                query = query.Where(s => s.OutputCode == outputCode.Value);

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            return await query.OrderByDescending(s => s.IndicatorsPerformance).ToListAsync();
        }

        private List<FrameworkSearchResultViewModel> BuildSearchResults(List<SubOutput> subOutputs, string searchString)
        {
            var results = new List<FrameworkSearchResultViewModel>();
            var searchTerm = searchString.ToLower();

            foreach (var subOutput in subOutputs)
            {
                var subOutputResult = new FrameworkSearchResultViewModel
                {
                    Framework = null, // Not used for suboutputs
                    Matches = new List<SearchMatch>()
                };

                // Check sub-output name
                if (subOutput.Name.ToLower().Contains(searchTerm))
                {
                    subOutputResult.Matches.Add(new SearchMatch
                    {
                        Type = "SubOutput",
                        Name = subOutput.Name,
                        NavigationUrl = Url.Action("Index", "Indicators", new { frameworkCode = subOutput.Output?.Outcome?.FrameworkCode, subOutputCode = subOutput.Code }),
                        Icon = "fas fa-cubes",
                        ParentPath = $"{subOutput.Output?.Outcome?.Framework?.Name} > {subOutput.Output?.Outcome?.Name} > {subOutput.Output?.Name}",
                        Code = subOutput.Code
                    });
                }

                // Check indicators
                foreach (var indicator in subOutput.Indicators ?? Enumerable.Empty<Indicator>())
                {
                    if (indicator.Name.ToLower().Contains(searchTerm))
                    {
                        subOutputResult.Matches.Add(new SearchMatch
                        {
                            Type = "Indicator",
                            Name = indicator.Name,
                            NavigationUrl = Url.Action("Index", "Measures", new { frameworkCode = subOutput.Output?.Outcome?.FrameworkCode, indicatorCode = indicator.IndicatorCode }),
                            Icon = "fas fa-chart-line",
                            ParentPath = $"{subOutput.Output?.Outcome?.Framework?.Name} > {subOutput.Output?.Outcome?.Name} > {subOutput.Output?.Name} > {subOutput.Name}",
                            Code = indicator.IndicatorCode
                        });
                    }

                    // Check project
                    if (indicator.Project?.ProjectName != null &&
                        indicator.Project.ProjectName.ToLower().Contains(searchTerm))
                    {
                        subOutputResult.Matches.Add(new SearchMatch
                        {
                            Type = "Project",
                            Name = indicator.Project.ProjectName,
                            NavigationUrl = Url.Action("Details", "Projects", new { id = indicator.Project.ProjectID }),
                            Icon = "fas fa-project-diagram",
                            ParentPath = $"{subOutput.Output?.Outcome?.Framework?.Name} > {subOutput.Output?.Outcome?.Name} > {subOutput.Output?.Name} > {subOutput.Name} > {indicator.Name}",
                            Code = indicator.Project.ProjectID,
                            Metadata = new Dictionary<string, object>
                            {
                                { "IndicatorName", indicator.Name }
                            }
                        });
                    }
                }

                // Only add to results if there are matches
                if (subOutputResult.Matches.Any())
                {
                    // Use subOutput as the grouping key
                    subOutputResult.Framework = new Framework
                    {
                        Code = subOutput.Code,
                        Name = subOutput.Name
                    };
                    results.Add(subOutputResult);
                }
            }

            return results;
        }
    }

}
