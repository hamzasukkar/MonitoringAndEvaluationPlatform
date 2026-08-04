using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Localization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class IndicatorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PlanService _planService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<IndicatorsController> _localizer;

        public IndicatorsController(ApplicationDbContext context, PlanService planService, UserManager<ApplicationUser> userManager, IStringLocalizer<IndicatorsController> localizer)
        {
            _context = context;
            _planService = planService;
            _userManager = userManager;
            _localizer = localizer;
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

        private async Task<bool> SubOutputBelongsToScopeAsync(int subOutputCode)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await _context.SubOutputs
                .Where(s => s.Code == subOutputCode)
                .AnyAsync(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
        }

        /// <summary>
        /// Resolves an indicator to its owning ministry via its SubOutput. Used by the
        /// chart/trend/table endpoints, which take an indicator code directly rather than
        /// a SubOutput code.
        /// </summary>
        private async Task<bool> IndicatorBelongsToScopeAsync(int indicatorCode)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await _context.Indicators
                .Where(i => i.IndicatorCode == indicatorCode)
                .AnyAsync(i => i.SubOutput.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
        }

        // GET: Indicators
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Index(int? frameworkCode, int? subOutputCode, string searchString,
            int? outcomeCode, int? outputCode, string performanceBand, string disbursementBand,
            string sortColumn = "Name", string sortDirection = "asc", int page = 1, int pageSize = 10)
        {
            // If navigating from a SubOutput link (no framework in the URL), derive the framework
            // so the cascade filter dropdowns can still populate.
            if (!frameworkCode.HasValue && subOutputCode.HasValue)
            {
                frameworkCode = await _context.SubOutputs
                    .Where(s => s.Code == subOutputCode.Value)
                    .Select(s => (int?)s.Output.Outcome.FrameworkCode)
                    .FirstOrDefaultAsync();
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["subOutputCode"] = subOutputCode;
            ViewData["frameworkCode"] = frameworkCode;

            // Get current user and check if they are a ministry user
            var currentUser = await _userManager.GetUserAsync(User);
            var isMinistryUser = User.IsInRole(UserRoles.MinistriesUser) || User.IsInRole(UserRoles.DataEntry);
            var userMinistryName = currentUser?.MinistryName;

            var indicators = _context.Indicators
                .Include(i => i.SubOutput)
                    .ThenInclude(so => so.Output)
                    .ThenInclude(o => o.Outcome)
                    .ThenInclude(oc => oc.Framework)
                .Include(i => i.Project)
                    .ThenInclude(p => p.Ministry)
                .Include(i => i.Project)
                    .ThenInclude(p => p.Phases)
                .AsQueryable();

            // Apply framework filter
            if (frameworkCode.HasValue)
            {
                indicators = indicators.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode);
            }

            // Apply subOutput filter
            if (subOutputCode.HasValue)
            {
                indicators = indicators.Where(i => i.SubOutputCode == subOutputCode);
            }

            // Filter by ministry if user is a ministry user
            if (isMinistryUser && !string.IsNullOrEmpty(userMinistryName))
            {
                indicators = indicators.Where(i =>
                    i.Project != null && i.Project.Ministry != null &&
                    (i.Project.Ministry.MinistryDisplayName_AR == userMinistryName ||
                     i.Project.Ministry.MinistryDisplayName_EN == userMinistryName ||
                     i.Project.Ministry.MinistryUserName == userMinistryName));
            }

            // Restrict to ancestor framework's ministry for non-admins
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                indicators = scopedMinistryCode is null
                    ? indicators.Where(_ => false)
                    : indicators.Where(i => i.SubOutput.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                indicators = indicators.Where(i =>
                    EF.Functions.Like(i.Name, $"%{searchString}%") ||
                    (i.SubOutput != null && EF.Functions.Like(i.SubOutput.Name, $"%{searchString}%")) ||
                    (i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%")));
            }

            // Apply hierarchy (Outcome / Output) filters
            if (outcomeCode.HasValue)
            {
                indicators = indicators.Where(i => i.SubOutput.Output.OutcomeCode == outcomeCode.Value);
            }
            if (outputCode.HasValue)
            {
                indicators = indicators.Where(i => i.SubOutput.OutputCode == outputCode.Value);
            }

            // Apply performance band filters (High >= 75, Medium 50-74, Low < 50)
            indicators = performanceBand?.ToLower() switch
            {
                "high" => indicators.Where(i => i.IndicatorsPerformance >= 75),
                "medium" => indicators.Where(i => i.IndicatorsPerformance >= 50 && i.IndicatorsPerformance < 75),
                "low" => indicators.Where(i => i.IndicatorsPerformance < 50),
                _ => indicators,
            };
            indicators = disbursementBand?.ToLower() switch
            {
                "high" => indicators.Where(i => i.DisbursementPerformance >= 75),
                "medium" => indicators.Where(i => i.DisbursementPerformance >= 50 && i.DisbursementPerformance < 75),
                "low" => indicators.Where(i => i.DisbursementPerformance < 50),
                _ => indicators,
            };

            // Sanitize paging/sorting inputs
            if (page < 1) page = 1;
            if (pageSize < 5 || pageSize > 100) pageSize = 10;
            sortDirection = sortDirection == "desc" ? "desc" : "asc";

            var totalRecords = await indicators.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            page = Math.Min(page, Math.Max(totalPages, 1));

            bool ascending = sortDirection == "asc";
            indicators = sortColumn?.ToLower() switch
            {
                "weight" => ascending
                    ? indicators.OrderBy(i => i.Weight).ThenBy(i => i.IndicatorCode)
                    : indicators.OrderByDescending(i => i.Weight).ThenBy(i => i.IndicatorCode),
                "indicatorsperformance" => ascending
                    ? indicators.OrderBy(i => i.IndicatorsPerformance).ThenBy(i => i.IndicatorCode)
                    : indicators.OrderByDescending(i => i.IndicatorsPerformance).ThenBy(i => i.IndicatorCode),
                "disbursementperformance" => ascending
                    ? indicators.OrderBy(i => i.DisbursementPerformance).ThenBy(i => i.IndicatorCode)
                    : indicators.OrderByDescending(i => i.DisbursementPerformance).ThenBy(i => i.IndicatorCode),
                _ => ascending
                    ? indicators.OrderBy(i => i.Name).ThenBy(i => i.IndicatorCode)
                    : indicators.OrderByDescending(i => i.Name).ThenBy(i => i.IndicatorCode),
            };

            var resultIndicators = await indicators
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pass suboutput name to view if viewing specific suboutput
            if (subOutputCode.HasValue)
            {
                ViewBag.SubOutputName = await _context.SubOutputs
                    .Where(s => s.Code == subOutputCode.Value)
                    .Select(s => s.Name)
                    .FirstOrDefaultAsync();
            }

            // Build the cascade filter dropdown option lists (framework-scoped)
            var outcomes = new List<Outcome>();
            var outputs = new List<Output>();
            var subOutputs = new List<SubOutput>();

            if (frameworkCode.HasValue)
            {
                outcomes = await _context.Outcomes
                    .Where(o => o.FrameworkCode == frameworkCode.Value)
                    .OrderBy(o => o.Name)
                    .ToListAsync();

                var outputsQuery = _context.Outputs
                    .Where(o => o.Outcome.FrameworkCode == frameworkCode.Value);
                if (outcomeCode.HasValue)
                    outputsQuery = outputsQuery.Where(o => o.OutcomeCode == outcomeCode.Value);
                outputs = await outputsQuery.OrderBy(o => o.Name).ToListAsync();

                var subOutputsQuery = _context.SubOutputs
                    .Where(so => so.Output.Outcome.FrameworkCode == frameworkCode.Value);
                if (outputCode.HasValue)
                    subOutputsQuery = subOutputsQuery.Where(so => so.OutputCode == outputCode.Value);
                else if (outcomeCode.HasValue)
                    subOutputsQuery = subOutputsQuery.Where(so => so.Output.OutcomeCode == outcomeCode.Value);

                if (!isAdmin)
                {
                    subOutputsQuery = scopedMinistryCode is null
                        ? subOutputsQuery.Where(_ => false)
                        : subOutputsQuery.Where(so => so.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
                }

                subOutputs = await subOutputsQuery.OrderBy(so => so.Name).ToListAsync();

                // Reuse the framework-level suboutput list for the inline create form dropdown
                if (!subOutputCode.HasValue)
                {
                    ViewBag.SubOutputsForCreate = await _context.SubOutputs
                        .Where(so => so.Output.Outcome.FrameworkCode == frameworkCode.Value)
                        .Where(so => isAdmin || (scopedMinistryCode != null && so.Output.Outcome.Framework.MinistryCode == scopedMinistryCode))
                        .OrderBy(so => so.Name)
                        .ToListAsync();
                }
            }

            var viewModel = new IndicatorListViewModel
            {
                Indicators = resultIndicators,
                FrameworkCode = frameworkCode,
                OutcomeCode = outcomeCode,
                OutputCode = outputCode,
                SubOutputCode = subOutputCode,
                SearchString = searchString,
                PerformanceBand = performanceBand,
                DisbursementBand = disbursementBand,
                Outcomes = outcomes,
                Outputs = outputs,
                SubOutputs = subOutputs,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages
            };

            return View(viewModel);
        }





        // GET: Indicators/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var indicator = await _context.Indicators
        //        .Include(i => i.SubOutput)
        //        .FirstOrDefaultAsync(m => m.Code == id);
        //    if (indicator == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(indicator);
        //}

        // GET: Indicators/Create
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(int? id)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<SubOutput> subOutputsQuery = _context.SubOutputs
                .Include(s => s.Output).ThenInclude(o => o.Outcome).ThenInclude(oc => oc.Framework);

            if (!isAdmin)
            {
                subOutputsQuery = scopedMinistryCode is null
                    ? subOutputsQuery.Where(_ => false)
                    : subOutputsQuery.Where(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            var subOutputs = await subOutputsQuery.ToListAsync();

            // Populate dropdown only if no SubOutput is preselected
            ViewData["SubOutputCode"] = id == null
                ? new SelectList(subOutputs, "Code", "Name")
                : new SelectList(subOutputs, "Code", "Name", id);

            // Pass the selected framework code to the view
            ViewBag.SelectedSubOutputCode = id;

            return View();
        }

        // POST: Indicators/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> Create(Indicator indicator)
        {
            ModelState.Remove(nameof(indicator.SubOutput));
            ModelState.Remove(nameof(indicator.Project));

            if (!await SubOutputBelongsToScopeAsync(indicator.SubOutputCode))
            {
                return Forbid();
            }

            // Server-owned values must never come from the request. These are computed from
            // measures and from RedistributeWeights below; binding them let a caller post
            // fabricated performance figures straight into the reporting hierarchy.
            indicator.IndicatorsPerformance = 0;
            indicator.DisbursementPerformance = 0;
            indicator.GAGRA = 0;
            indicator.GAGRR = 0;
            indicator.Weight = 1;

            // Check if indicator name already exists within the same suboutput
            var existingIndicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.SubOutputCode == indicator.SubOutputCode &&
                                          i.Name.ToLower() == indicator.Name.Trim().ToLower());
            if (existingIndicator != null)
            {
                ModelState.AddModelError("Name", _localizer["An indicator with this name already exists in this suboutput."]);
                ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
                return View(indicator);
            }

            if (ModelState.IsValid)
            {
                // Add the new indicator
                _context.Add(indicator);
                await _context.SaveChangesAsync();

                // Update related entities
                await UpdateSubOutputPerformance(indicator.SubOutputCode);
                // Recalculate weights
                await RedistributeWeights(indicator.SubOutputCode);

                return RedirectToAction(nameof(Index), new { frameworkCode = indicator.SubOutput.Output.Outcome.FrameworkCode, subOutputCode = indicator.SubOutputCode });
            }

            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            return View(indicator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateInline(string Name, int Target, int SubOutputCode)
        {
            if (string.IsNullOrWhiteSpace(Name) || Target <= 0)
            {
                TempData["Error"] = _localizer["Name and Target are required and must be valid."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            if (!await SubOutputBelongsToScopeAsync(SubOutputCode))
            {
                return Forbid();
            }

            // Check if indicator name already exists within the same suboutput
            var existingIndicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.SubOutputCode == SubOutputCode &&
                                          i.Name.ToLower() == Name.Trim().ToLower());
            if (existingIndicator != null)
            {
                TempData["Error"] = _localizer["An indicator with this name already exists in this suboutput."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            var indicator = new Indicator
            {
                Name = Name,
                Target = Target,
                SubOutputCode = SubOutputCode
            };

            _context.Indicators.Add(indicator);
            await _context.SaveChangesAsync();


            // Update related entities
            await UpdateSubOutputPerformance(indicator.SubOutputCode);
            // Recalculate weights
            await RedistributeWeights(indicator.SubOutputCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = indicator.SubOutput.Output.Outcome.FrameworkCode, subOutputCode = indicator.SubOutputCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateInlineAjax(string name, int subOutputCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = _localizer["Indicator name is required."].Value });
                }

                if (!await SubOutputBelongsToScopeAsync(subOutputCode))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this suboutput."].Value });
                }

                // Check if indicator name already exists within the same suboutput
                var existingIndicator = await _context.Indicators
                    .FirstOrDefaultAsync(i => i.SubOutputCode == subOutputCode &&
                                              i.Name.ToLower() == name.Trim().ToLower());
                if (existingIndicator != null)
                {
                    return Json(new { success = false, message = _localizer["An indicator with this name already exists in this suboutput."].Value });
                }

                var indicator = new Indicator
                {
                    Name = name.Trim(),
                    Target = 0,
                    SubOutputCode = subOutputCode,
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0
                };

                _context.Indicators.Add(indicator);
                await _context.SaveChangesAsync();

                // Update related entities
                await UpdateSubOutputPerformance(indicator.SubOutputCode);
                // Recalculate weights
                await RedistributeWeights(indicator.SubOutputCode);

                return Json(new
                {
                    success = true,
                    indicator = new
                    {
                        code = indicator.IndicatorCode,
                        name = indicator.Name,
                        weight = Math.Round(indicator.Weight, 2),
                        indicatorsPerformance = Math.Round(indicator.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(indicator.DisbursementPerformance, 2)
                    },
                    message = _localizer["Indicator created successfully!"].Value
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = _localizer["Could not create indicator. Please try again."].Value });
            }
        }

        /// <summary>
        /// This is the NEW action that handles the "Add & Create Project" button.
        /// It creates the Indicator and then redirects to the Create action in the ProjectsController,
        /// passing the new Indicator's ID and Name for auto-filling the project form.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddIndicator)]
        public async Task<IActionResult> CreateAndRedirectToProject(string Name, int Target, int SubOutputCode)
        {
            if (string.IsNullOrWhiteSpace(Name) || Target <= 0)
            {
                TempData["Error"] = _localizer["Name and Target are required and must be valid."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            if (!await SubOutputBelongsToScopeAsync(SubOutputCode))
            {
                return Forbid();
            }

            // Check if indicator name already exists within the same suboutput
            var existingIndicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.SubOutputCode == SubOutputCode &&
                                          i.Name.ToLower() == Name.Trim().ToLower());
            if (existingIndicator != null)
            {
                TempData["Error"] = _localizer["An indicator with this name already exists in this suboutput."].Value;
                return RedirectToAction("Index", new { subOutputCode = SubOutputCode });
            }

            var indicator = new Indicator
            {
                Name = Name,
                Target = Target,
                SubOutputCode = SubOutputCode
            };

            _context.Indicators.Add(indicator);
            await _context.SaveChangesAsync(); // This saves the indicator and populates its ID

            // Update related entities
            await UpdateSubOutputPerformance(indicator.SubOutputCode);
            // Recalculate weights
            await RedistributeWeights(indicator.SubOutputCode);

            TempData["Success"] = _localizer["Indicator created. You can now add project details."].Value;

            // Redirect to the "Create" action in the "Projects" controller.
            // Pass the newly created indicator's ID and Name so the project can be associated with it
            // and the project name can be auto-filled.
            return RedirectToAction("Create", "Projects", new { indicatorId = indicator.IndicatorCode, indicatorName = indicator.Name });
        }

        // تحديث SubOutput بناءً على Indicators
        public async Task UpdateSubOutputPerformance(int subOutputCode)
        {
            var subOutput = await _context.SubOutputs
                .Include(s => s.Indicators)
                .FirstOrDefaultAsync(s => s.Code == subOutputCode);

            if (subOutput == null)
                throw new Exception("SubOutput not found");

            double totalWeight = subOutput.Indicators.Sum(i => i.Weight);

            if (totalWeight <= 0) totalWeight = subOutput.Indicators.Count; // fallback للأوزان المتساوية

            double weightedPerformance = subOutput.Indicators.Sum(i => i.IndicatorsPerformance * i.Weight / totalWeight);

            subOutput.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.SubOutputs.Update(subOutput);
            await _context.SaveChangesAsync();

            await UpdateOutputPerformance(subOutput.OutputCode);
        }

        // تحديث Output بناءً على SubOutputs
        public async Task UpdateOutputPerformance(int outputCode)
        {
            var output = await _context.Outputs
                .Include(o => o.SubOutputs)
                .FirstOrDefaultAsync(o => o.Code == outputCode);

            if (output == null)
                throw new Exception("Output not found");

            double totalWeight = output.SubOutputs.Sum(s => s.Weight);

            if (totalWeight <= 0) totalWeight = output.SubOutputs.Count;

            double weightedPerformance = output.SubOutputs.Sum(s => s.IndicatorsPerformance * s.Weight / totalWeight);

            output.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.Outputs.Update(output);
            await _context.SaveChangesAsync();

            await UpdateOutcomePerformance(output.OutcomeCode);
        }

        // تحديث Outcome بناءً على Outputs
        public async Task UpdateOutcomePerformance(int outcomeCode)
        {
            var outcome = await _context.Outcomes
                .Include(o => o.Outputs)
                .FirstOrDefaultAsync(o => o.Code == outcomeCode);

            if (outcome == null)
                throw new Exception("Outcome not found");

            double totalWeight = outcome.Outputs.Sum(o => o.Weight);

            if (totalWeight <= 0) totalWeight = outcome.Outputs.Count;

            double weightedPerformance = outcome.Outputs.Sum(o => o.IndicatorsPerformance * o.Weight / totalWeight);

            outcome.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.Outcomes.Update(outcome);
            await _context.SaveChangesAsync();

            await UpdateFrameworkPerformance(outcome.FrameworkCode);
        }

        // تحديث Framework بناءً على Outcomes
        public async Task UpdateFrameworkPerformance(int frameworkCode)
        {
            var framework = await _context.Frameworks
                .Include(f => f.Outcomes)
                .FirstOrDefaultAsync(f => f.Code == frameworkCode);

            if (framework == null)
                throw new Exception("Framework not found");

            double totalWeight = framework.Outcomes.Sum(o => o.Weight);

            if (totalWeight <= 0) totalWeight = framework.Outcomes.Count;

            double weightedPerformance = framework.Outcomes.Sum(o => o.IndicatorsPerformance * o.Weight / totalWeight);

            framework.IndicatorsPerformance = Math.Round(weightedPerformance, 2);

            _context.Frameworks.Update(framework);
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> InlineEditName(int id, [FromBody] JsonElement data)
        {
            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null) return NotFound();

            if (!await SubOutputBelongsToScopeAsync(indicator.SubOutputCode))
            {
                return Forbid();
            }

            var newName = data.GetProperty("name").GetString();
            indicator.Name = newName;
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null)
            {
                return NotFound();
            }

            if (!await SubOutputBelongsToScopeAsync(indicator.SubOutputCode))
            {
                return Forbid();
            }

            if (indicator.ProjectID.HasValue)
            {
                var monitoringService = new MonitoringService(_context);
                await monitoringService.DeleteProjectAndRecalculateAsync(indicator.ProjectID.Value);
            }
            else
            {
                _context.Indicators.Remove(indicator);
                await _context.SaveChangesAsync();
            }

            await RedistributeWeights(indicator.SubOutputCode);
            await _planService.RecalculatePerformanceAfterIndicatorDeletion(indicator);

            return Ok();
        }



        // GET: Indicators/Edit/5
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicator = await _context.Indicators.FindAsync(id);
            if (indicator == null)
            {
                return NotFound();
            }

            if (!await SubOutputBelongsToScopeAsync(indicator.SubOutputCode))
            {
                return Forbid();
            }

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<SubOutput> subOutputsQuery = _context.SubOutputs
                .Include(s => s.Output).ThenInclude(o => o.Outcome).ThenInclude(oc => oc.Framework);
            if (!isAdmin)
            {
                subOutputsQuery = scopedMinistryCode is null
                    ? subOutputsQuery.Where(_ => false)
                    : subOutputsQuery.Where(s => s.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }
            ViewData["SubOutputCode"] = new SelectList(await subOutputsQuery.ToListAsync(), "Code", "Name", indicator.SubOutputCode);

            if (indicator.ProjectID.HasValue)
            {
                var project = await _context.Projects.FindAsync(indicator.ProjectID.Value);
                ViewBag.ProjectName = project?.ProjectName;
            }
            return View(indicator);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> Edit(int id,Indicator indicator)
        {
            if (id != indicator.IndicatorCode)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(indicator.SubOutput));
            ModelState.Remove(nameof(indicator.Project));
            ModelState.Remove(nameof(indicator.ProjectID));

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Indicators.FindAsync(id);
                    if (existing == null) return NotFound();

                    if (!await SubOutputBelongsToScopeAsync(existing.SubOutputCode) ||
                        !await SubOutputBelongsToScopeAsync(indicator.SubOutputCode))
                    {
                        return Forbid();
                    }

                    existing.Name = indicator.Name;
                    existing.SubOutputCode = indicator.SubOutputCode;
                    // ProjectID is immutable — not overwritten

                    await _context.SaveChangesAsync();

                    // Update related entities
                    await UpdateSubOutputPerformance(existing.SubOutputCode);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IndicatorExists(indicator.IndicatorCode))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["SubOutputCode"] = new SelectList(_context.SubOutputs, "Code", "Name", indicator.SubOutputCode);
            if (indicator.ProjectID.HasValue)
            {
                var project = await _context.Projects.FindAsync(indicator.ProjectID.Value);
                ViewBag.ProjectName = project?.ProjectName;
            }
            return View(indicator);
        }

        // GET: Indicators/Delete/5
        [Permission(Permissions.DeleteIndicator)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var indicator = await _context.Indicators
                .Include(i => i.SubOutput)
                .FirstOrDefaultAsync(m => m.IndicatorCode == id);
            if (indicator == null)
            {
                return NotFound();
            }

            if (!await SubOutputBelongsToScopeAsync(indicator.SubOutputCode))
            {
                return Forbid();
            }

            return View(indicator);
        }
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _context.Indicators
                .Include(i => i.SubOutput)
                .ThenInclude(so => so.Output)
                .ThenInclude(o => o.Outcome)
                .ThenInclude(oc => oc.Framework)
                .FirstOrDefaultAsync(i => i.IndicatorCode == id);

            if (indicator == null)
                return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && indicator.SubOutput?.Output?.Outcome?.Framework?.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            // Build the hierarchy model
            var hierarchy = new List<(string Name, string Type, int Code)>
    {
        (indicator.SubOutput.Output.Outcome.Framework.Name, "Framework", indicator.SubOutput.Output.Outcome.Framework.Code),
        (indicator.SubOutput.Output.Outcome.Name, "Outcome", indicator.SubOutput.Output.Outcome.Code),
        (indicator.SubOutput.Output.Name, "Output", indicator.SubOutput.Output.Code),
        (indicator.SubOutput.Name, "SubOutput", indicator.SubOutput.Code),
        (indicator.Name, "Indicator", indicator.IndicatorCode)
    };

            // Measures are now linked to ProjectPhases; get them via the indicator's project
            var measures = indicator.ProjectID.HasValue
                ? await _context.Measures
                    .Include(m => m.ProjectPhase)
                    .Where(m => m.ProjectPhase.ProjectID == indicator.ProjectID.Value)
                    .OrderBy(m => m.Date)
                    .ToListAsync()
                : new List<Measure>();

            var labels = new List<string>();
            var realData = new List<double>();
            var historicalData = new List<double>();
            var requiredData = new List<double>();

            foreach (var measure in measures)
            {
                labels.Add(measure.Date.ToString());
                realData.Add(measure.Value);
                historicalData.Add(measure.Value + 20);
                requiredData.Add(measure.Value + 10);
            }

            var chartDataViewModel = new ChartDataViewModel
            {
                Labels = labels,
                RealData = realData,
                HistoricalData = historicalData,
                RequiredData = requiredData
            };

            var model = new IndicatorDetailsViewModel
            {
                Indicator = indicator,
                Hierarchy = hierarchy,
                Measures = measures,
                ChartDataViewModel = chartDataViewModel
            };

            return View(model);
        }


        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> GetMeasureChartData(int indicatorCode)
        {
            if (!await IndicatorBelongsToScopeAsync(indicatorCode))
            {
                return Forbid();
            }

            // Get indicator to find its project
            var indicatorForChart = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

            var data = indicatorForChart?.ProjectID.HasValue == true
                ? await _context.Measures
                    .Where(m => m.ProjectPhase.ProjectID == indicatorForChart.ProjectID.Value)
                    .OrderBy(m => m.Date)
                    .Select(m => new { date = m.Date.ToString("yyyy-MM-dd"), value = m.Value })
                    .ToListAsync()
                : new List<object>().Select(x => new { date = "", value = 0.0 }).ToList();

            var targetValue = indicatorForChart?.Target ?? 0;
            var target = new[] { new { date = "baseline", value = targetValue } };

            var result = new { Real = data, Target = target };

            return Json(result);
        }

        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> TrendData(int indicatorCode)
        {
            if (!await IndicatorBelongsToScopeAsync(indicatorCode))
            {
                return Forbid();
            }

            var indicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

            if (indicator == null || !indicator.ProjectID.HasValue)
                return Json(new { monthly = Array.Empty<object>(), quarterly = Array.Empty<object>(), phases = Array.Empty<object>() });

            var measures = await _context.Measures
                .Include(m => m.ProjectPhase)
                .Where(m => m.ProjectPhase.ProjectID == indicator.ProjectID.Value)
                .OrderBy(m => m.Date)
                .ToListAsync();

            if (!measures.Any())
                return Json(new { monthly = Array.Empty<object>(), quarterly = Array.Empty<object>(), phases = Array.Empty<object>() });

            var monthly = measures
                .GroupBy(m => new { m.Date.Year, m.Date.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    period = $"{g.Key.Year}-{g.Key.Month:D2}",
                    label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    performance = Math.Round(g.Average(m => m.Value), 1)
                }).ToList();

            var quarterly = measures
                .GroupBy(m => new { m.Date.Year, Quarter = (m.Date.Month - 1) / 3 + 1 })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Quarter)
                .Select(g => new
                {
                    period = $"{g.Key.Year}-Q{g.Key.Quarter}",
                    label = $"Q{g.Key.Quarter} {g.Key.Year}",
                    performance = Math.Round(g.Average(m => m.Value), 1)
                }).ToList();

            var phases = measures
                .GroupBy(m => new { m.ProjectPhase.Id, m.ProjectPhase.Name })
                .Select(pg => new
                {
                    name = pg.Key.Name,
                    monthly = pg
                        .GroupBy(m => new { m.Date.Year, m.Date.Month })
                        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                        .Select(g => new
                        {
                            label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                            performance = Math.Round(g.Average(m => m.Value), 1)
                        }).ToList(),
                    quarterly = pg
                        .GroupBy(m => new { m.Date.Year, Quarter = (m.Date.Month - 1) / 3 + 1 })
                        .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Quarter)
                        .Select(g => new
                        {
                            label = $"Q{g.Key.Quarter} {g.Key.Year}",
                            performance = Math.Round(g.Average(m => m.Value), 1)
                        }).ToList()
                }).ToList();

            return Json(new { monthly, quarterly, phases });
        }

        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> MeasureTablePartial(int indicatorCode)
        {
            if (!await IndicatorBelongsToScopeAsync(indicatorCode))
            {
                return Forbid();
            }

            var indicatorForTable = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorCode == indicatorCode);

            var measures = indicatorForTable?.ProjectID.HasValue == true
                ? await _context.Measures
                    .Where(m => m.ProjectPhase.ProjectID == indicatorForTable.ProjectID.Value)
                    .OrderBy(m => m.Date)
                    .ToListAsync()
                : new List<Measure>();

            return PartialView("_MeasureTablePartial", measures);
        }

   
        private bool IndicatorExists(int id)
        {
            return _context.Indicators.Any(e => e.IndicatorCode == id);
        }

        [Permission(Permissions.ReadIndicators)]
        public IActionResult Chart()
        {
            var viewModel = new ChartDataViewModel
            {
                Labels = new List<string>
            {
                "2019-01-01", "2022-01-01", "2030-02-01", "2030-03-01",
                "2030-04-01", "2030-05-01", "2030-06-01"
            },
                RealData = new List<double> { 80, 85, 90, 95, 98, 99, 100 },
                HistoricalData = new List<double> { 80, 82, 83, 85, 87, 89, 91 },
                RequiredData = new List<double> { 80, 83, 86, 89, 92, 95, 100 }
            };

            return View(viewModel);
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
                _context.Entry(i).State = EntityState.Modified;
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

       // GET: Indicators/AdjustWeights/5
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> AdjustWeights(int frameworkCode, int subOutputCode)
        {
            if (!await SubOutputBelongsToScopeAsync(subOutputCode))
            {
                return Forbid();
            }

            var indicators = await _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .ToListAsync();

            var model = indicators.Select(i => new IndicatorViewModel
            {
                Code = i.IndicatorCode,
                Name = i.Name,
                Weight = i.Weight
            }).ToList();

            ViewBag.SubOutputCode = subOutputCode;
            ViewBag.FrameworkCode = frameworkCode;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyIndicator)]
        public async Task<IActionResult> AdjustWeights(List<IndicatorViewModel> model, int frameworkCode, int subOutputCode)
        {
            if (!await SubOutputBelongsToScopeAsync(subOutputCode))
            {
                return Forbid();
            }

            double totalWeight = model.Sum(i => i.Weight);

            if (Math.Abs(totalWeight - 100.0) > 0.01)
            {
                ModelState.AddModelError("", _localizer["Total weight must equal 100%."]);
                ViewBag.SubOutputCode = subOutputCode;
                ViewBag.FrameworkCode = frameworkCode;
                return View(model);
            }

            foreach (var vm in model)
            {
                var indicator = await _context.Indicators.FindAsync(vm.Code);
                if (indicator != null)
                {
                    indicator.Weight = vm.Weight;
                    _context.Update(indicator);
                }
            }

            await _context.SaveChangesAsync();

            // بعد حفظ الأوزان الجديدة، إعادة حساب أداء SubOutput بناء على الأوزان
            await UpdateSubOutputPerformance(subOutputCode);

            return RedirectToAction(nameof(Index), new { frameworkCode = frameworkCode, subOutputCode = subOutputCode });
        }

        // GET: Indicators/IndicatorAndProject
        [Permission(Permissions.ReadIndicators)]
        public IActionResult IndicatorAndProject(int? projectId, int? frameworkCode, int? subOutputCode, string searchString)
        {
            // Merged into Index — redirect to preserve any bookmarked/linked URLs
            return RedirectToAction("Index", new { frameworkCode, subOutputCode, searchString, projectId });
        }

        // GET: Demo page for project display options
        public IActionResult ProjectDisplayOptions()
        {
            return View();
        }

        // GET: Indicators/IndicatorAndProjectTable
        [Permission(Permissions.ReadIndicators)]
        public IActionResult IndicatorAndProjectTable(int? projectId, int? frameworkCode, int? subOutputCode, string searchString)
        {
            // Merged into Index — redirect to preserve any bookmarked/linked URLs
            return RedirectToAction("Index", new { frameworkCode, subOutputCode, searchString, projectId });
        }

        // GET: Indicators/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> ExportExcel(int? frameworkCode, int? subOutputCode, string searchString,
            int? outcomeCode, int? outputCode, string performanceBand, string disbursementBand)
        {
            var indicators = await GetFilteredIndicators(frameworkCode, subOutputCode, searchString,
                outcomeCode, outputCode, performanceBand, disbursementBand);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Indicators"].Value);

            if (isRtl) worksheet.RightToLeft = true;

            worksheet.Cell(1, 1).Value = _localizer["Indicator Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Weight"].Value + " (%)";
            worksheet.Cell(1, 3).Value = _localizer["Target"].Value;
            worksheet.Cell(1, 4).Value = _localizer["Indicators Performance"].Value + " (%)";
            worksheet.Cell(1, 5).Value = _localizer["SubOutput"].Value;

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var indicator in indicators)
            {
                worksheet.Cell(row, 1).Value = indicator.Name;
                worksheet.Cell(row, 2).Value = Math.Round(indicator.Weight, 2);
                worksheet.Cell(row, 3).Value = indicator.Target;
                worksheet.Cell(row, 4).Value = Math.Round(indicator.IndicatorsPerformance, 2);
                worksheet.Cell(row, 5).Value = indicator.SubOutput?.Name ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            var dataRange = worksheet.Range(1, 1, row - 1, 5);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "المشاريع" : "Indicators";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: Indicators/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadIndicators)]
        public async Task<IActionResult> ExportPdf(int? frameworkCode, int? subOutputCode, string searchString,
            int? outcomeCode, int? outputCode, string performanceBand, string disbursementBand)
        {
            var indicators = await GetFilteredIndicators(frameworkCode, subOutputCode, searchString,
                outcomeCode, outputCode, performanceBand, disbursementBand);
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
                                col.Item().Text(_localizer["Indicators"].Value).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"{_localizer["Generated on"].Value}: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicator Name"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Weight"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Target"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["Indicators Performance"].Value).FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text(_localizer["SubOutput"].Value).FontColor(Colors.White).Bold();
                        });

                        foreach (var indicator in indicators)
                        {
                            var performance = Math.Round(indicator.IndicatorsPerformance, 2);

                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(indicator.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{Math.Round(indicator.Weight, 2)}%");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(indicator.Target.ToString());
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text($"{performance}%").FontColor(GetPerformanceColor(performance));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(indicator.SubOutput?.Name ?? "");
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
            var filePrefix = isRtl ? "المشاريع" : "Indicators";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<Indicator>> GetFilteredIndicators(int? frameworkCode, int? subOutputCode, string searchString = null,
            int? outcomeCode = null, int? outputCode = null, string performanceBand = null, string disbursementBand = null)
        {
            var query = _context.Indicators.Include(i => i.SubOutput).AsQueryable();

            if (frameworkCode.HasValue)
                query = query.Where(i => i.SubOutput.Output.Outcome.FrameworkCode == frameworkCode.Value);

            if (outcomeCode.HasValue)
                query = query.Where(i => i.SubOutput.Output.OutcomeCode == outcomeCode.Value);

            if (outputCode.HasValue)
                query = query.Where(i => i.SubOutput.OutputCode == outputCode.Value);

            if (subOutputCode.HasValue)
                query = query.Where(i => i.SubOutputCode == subOutputCode.Value);

            // Filter by ministry if user is a ministry user (same rules as Index)
            var currentUser = await _userManager.GetUserAsync(User);
            var isMinistryUser = User.IsInRole(UserRoles.MinistriesUser) || User.IsInRole(UserRoles.DataEntry);
            var userMinistryName = currentUser?.MinistryName;

            if (isMinistryUser && !string.IsNullOrEmpty(userMinistryName))
            {
                query = query.Where(i =>
                    i.Project != null && i.Project.Ministry != null &&
                    (i.Project.Ministry.MinistryDisplayName_AR == userMinistryName ||
                     i.Project.Ministry.MinistryDisplayName_EN == userMinistryName ||
                     i.Project.Ministry.MinistryUserName == userMinistryName));
            }

            // Restrict to ancestor framework's ministry for non-admins
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(i => i.SubOutput.Output.Outcome.Framework.MinistryCode == scopedMinistryCode);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i =>
                    EF.Functions.Like(i.Name, $"%{searchString}%") ||
                    (i.SubOutput != null && EF.Functions.Like(i.SubOutput.Name, $"%{searchString}%")) ||
                    (i.Project != null && EF.Functions.Like(i.Project.ProjectName, $"%{searchString}%")));
            }

            query = performanceBand?.ToLower() switch
            {
                "high" => query.Where(i => i.IndicatorsPerformance >= 75),
                "medium" => query.Where(i => i.IndicatorsPerformance >= 50 && i.IndicatorsPerformance < 75),
                "low" => query.Where(i => i.IndicatorsPerformance < 50),
                _ => query,
            };
            query = disbursementBand?.ToLower() switch
            {
                "high" => query.Where(i => i.DisbursementPerformance >= 75),
                "medium" => query.Where(i => i.DisbursementPerformance >= 50 && i.DisbursementPerformance < 75),
                "low" => query.Where(i => i.DisbursementPerformance < 50),
                _ => query,
            };

            return await query.OrderByDescending(i => i.IndicatorsPerformance).ToListAsync();
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


}

