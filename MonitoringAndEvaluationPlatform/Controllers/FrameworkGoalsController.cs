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
using MonitoringAndEvaluationPlatform.Enums;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Localization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class FrameworkGoalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<FrameworkGoalsController> _localizer;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUploadValidationService _uploadValidation;

        public FrameworkGoalsController(
            ApplicationDbContext context,
            IStringLocalizer<FrameworkGoalsController> localizer,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            IUploadValidationService uploadValidation)
        {
            _context = context;
            _localizer = localizer;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _uploadValidation = uploadValidation;
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

        private async Task<List<Framework>> GetScopedFrameworksAsync()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<Framework> query = _context.Frameworks;
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(f => f.MinistryCode == scopedMinistryCode);
            }
            return await query.ToListAsync();
        }

        private async Task<IQueryable<FrameworkGoal>> GetScopedGoalsQueryAsync()
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            IQueryable<FrameworkGoal> query = _context.FrameworkGoals;
            if (!isAdmin)
            {
                query = scopedMinistryCode is null
                    ? query.Where(_ => false)
                    : query.Where(fg => fg.Framework.MinistryCode == scopedMinistryCode);
            }
            return query;
        }

        private async Task<bool> GoalBelongsToScopeAsync(int goalId)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await _context.FrameworkGoals
                .Where(fg => fg.ID == goalId)
                .AnyAsync(fg => fg.Framework.MinistryCode == scopedMinistryCode);
        }

        // GET: FrameworkGoals
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .Include(fg => fg.YearlyValues)
                .Include(fg => fg.ManualExpectedTargets)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/ProgressView
        public async Task<IActionResult> ProgressView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/CircularGaugeView
        public async Task<IActionResult> CircularGaugeView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/TimelineView
        public async Task<IActionResult> TimelineView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/ValueProgressView
        public async Task<IActionResult> ValueProgressView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/StatusBadgesView
        public async Task<IActionResult> StatusBadgesView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/MetricsGridView
        public async Task<IActionResult> MetricsGridView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/ChartsView
        public async Task<IActionResult> ChartsView(int? frameworkCode)
        {
            ViewBag.Frameworks = await GetScopedFrameworksAsync();

            var goalsQuery = await GetScopedGoalsQueryAsync();
            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
            return View(goals);
        }

        // POST: FrameworkGoals/CreateInline - AJAX endpoint for inline creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.AddStrategy)]
        public async Task<IActionResult> CreateInline(FrameworkGoalCreateModel model)
        {
            try
            {
                // Manual validation since we're using string types for decimal values
                if (string.IsNullOrWhiteSpace(model.Name))
                {
                    return Json(new { success = false, message = _localizer["Please enter a goal name."].Value });
                }

                if (string.IsNullOrWhiteSpace(model.BaseValueForStartingYear) ||
                    string.IsNullOrWhiteSpace(model.BaseValueForCurrentYear) ||
                    string.IsNullOrWhiteSpace(model.TargetValue))
                {
                    return Json(new { success = false, message = _localizer["Please fill in all required fields."].Value });
                }

                // Parse decimal values — reject the request rather than silently saving 0 for a typo
                if (!FrameworkGoalCreateModel.TryParseDecimal(model.BaseValueForStartingYear, out var baseValueStart) ||
                    !FrameworkGoalCreateModel.TryParseDecimal(model.BaseValueForCurrentYear, out var baseValueCurrent) ||
                    !FrameworkGoalCreateModel.TryParseDecimal(model.TargetValue, out var targetValue))
                {
                    return Json(new { success = false, message = _localizer["Please enter valid numeric values."].Value });
                }

                // Validate framework exists
                var framework = await _context.Frameworks.FindAsync(model.FrameworkCode);
                if (framework == null)
                {
                    return Json(new { success = false, message = _localizer["Invalid framework selected."].Value });
                }

                var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
                if (!isAdmin && framework.MinistryCode != scopedMinistryCode)
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this framework."].Value });
                }

                // Validate year order
                if (model.StartingYear >= model.CurrentYear || model.CurrentYear >= model.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Years must be in ascending order: Starting Year < Current Year < Target Year."].Value });
                }

                // A quantitative goal is meaningless without a unit of measure
                if (model.GoalType == FrameworkGoalType.Quantitative && string.IsNullOrWhiteSpace(model.Unit))
                {
                    return Json(new { success = false, message = _localizer["Please enter a unit of measure for a quantitative goal."].Value });
                }

                // Parse and validate manual expected targets up front, before the goal is saved,
                // so an unparseable value rejects the whole request instead of leaving a goal
                // half-created with some manual targets missing.
                var manualTargets = new List<(int Year, double Value)>();
                if (model.ManualYearlyTargets && model.ManualTargetYears != null)
                {
                    for (int i = 0; i < model.ManualTargetYears.Count; i++)
                    {
                        var year = model.ManualTargetYears[i];
                        // Skip years that are not intermediate (starting, current, target are auto-calculated)
                        if (year <= model.StartingYear || year >= model.TargetYear || year == model.CurrentYear)
                            continue;

                        var rawValue = i < model.ManualTargetValues.Count ? model.ManualTargetValues[i] : "0";
                        if (!FrameworkGoalCreateModel.TryParseDecimal(rawValue, out var expectedValue))
                        {
                            return Json(new { success = false, message = _localizer["Please enter a valid numeric value for the manual target of year {0}."].Value.Replace("{0}", year.ToString()) });
                        }

                        manualTargets.Add((year, expectedValue));
                    }
                }

                // Create new framework goal
                var frameworkGoal = new FrameworkGoal
                {
                    Name = model.Name.Trim(),
                    StartingYear = model.StartingYear,
                    BaseValueForStartingYear = baseValueStart,
                    CurrentYear = model.CurrentYear,
                    BaseValueForCurrentYear = baseValueCurrent,
                    TargetYear = model.TargetYear,
                    TargetValue = targetValue,
                    FrameworkCode = model.FrameworkCode,
                    ManualYearlyTargets = model.ManualYearlyTargets,
                    GoalType = model.GoalType,
                    // Normalized so a qualitative goal never keeps a stale unit from a toggled form
                    Unit = model.GoalType == FrameworkGoalType.Quantitative ? model.Unit!.Trim() : null
                };

                _context.Add(frameworkGoal);
                await _context.SaveChangesAsync();

                // Save manual expected targets (already validated above)
                if (manualTargets.Count > 0)
                {
                    foreach (var (year, expectedValue) in manualTargets)
                    {
                        _context.FrameworkGoalManualExpectedTargets.Add(new FrameworkGoalManualExpectedTarget
                        {
                            FrameworkGoalID = frameworkGoal.ID,
                            Year = year,
                            ExpectedTargetValue = expectedValue
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // Return the created goal data for frontend update
                return Json(new
                {
                    success = true,
                    goal = new
                    {
                        id = frameworkGoal.ID,
                        name = frameworkGoal.Name,
                        frameworkName = framework.Name,
                        startingYear = frameworkGoal.StartingYear,
                        baseValueForStartingYear = frameworkGoal.BaseValueForStartingYear,
                        currentYear = frameworkGoal.CurrentYear,
                        baseValueForCurrentYear = frameworkGoal.BaseValueForCurrentYear,
                        targetYear = frameworkGoal.TargetYear,
                        targetValue = frameworkGoal.TargetValue,
                        goalType = (int)frameworkGoal.GoalType,
                        unit = frameworkGoal.Unit,
                        displayUnit = frameworkGoal.DisplayUnit
                    },
                    message = _localizer["Framework Goal created successfully!"].Value
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while creating the framework goal. Please try again."].Value });
            }
        }

        // POST: FrameworkGoals/UpdateCurrentYear - Update current year and its value
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UpdateCurrentYear(int id, int currentYear, double baseValueForCurrentYear)
        {
            try
            {
                var goal = await _context.FrameworkGoals
                    .Include(g => g.YearlyValues)
                    .FirstOrDefaultAsync(g => g.ID == id);

                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(id))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this goal."].Value });
                }

                // Validate year order (current year must be after starting year and up to target year)
                if (currentYear <= goal.StartingYear || currentYear > goal.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Current year must be between starting year and target year."].Value });
                }

                // If current year is changing, save the old current year value as historical data
                if (currentYear != goal.CurrentYear)
                {
                    // Check if historical value already exists for the old current year
                    var existingValue = goal.YearlyValues.FirstOrDefault(yv => yv.Year == goal.CurrentYear);
                    if (existingValue == null)
                    {
                        // Save the old current year value as historical data
                        var historicalValue = new FrameworkGoalYearlyValue
                        {
                            FrameworkGoalID = goal.ID,
                            Year = goal.CurrentYear,
                            ActualValue = goal.BaseValueForCurrentYear,
                            DateRecorded = DateTime.Now
                        };
                        _context.FrameworkGoalYearlyValues.Add(historicalValue);
                    }
                }

                // Update values
                goal.CurrentYear = currentYear;
                goal.BaseValueForCurrentYear = baseValueForCurrentYear;

                _context.Update(goal);
                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = _localizer["Current year updated successfully!"].Value,
                    goal = new
                    {
                        id = goal.ID,
                        currentYear = goal.CurrentYear,
                        baseValueForCurrentYear = goal.BaseValueForCurrentYear,
                        annualDiscountRate = goal.AnnualDiscountRate,
                        amountOfReduction = goal.AmountOfReduction,
                        expectedValue = goal.ExpectedValueForCurrentYear,
                        progressRate = goal.ProgressRate
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while updating the goal."].Value });
            }
        }

        // POST: FrameworkGoals/UpdateGoalValues - Update current year and historical values
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UpdateGoalValues([FromBody] UpdateGoalValuesModel model)
        {
            try
            {
                var goal = await _context.FrameworkGoals
                    .Include(g => g.YearlyValues)
                    .FirstOrDefaultAsync(g => g.ID == model.Id);

                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(model.Id))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this goal."].Value });
                }

                // Validate current year is within range (current year must be after starting year and up to target year)
                if (model.CurrentYear <= goal.StartingYear || model.CurrentYear > goal.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Current year must be between starting year and target year."].Value });
                }

                // If current year is changing, save the old current year value as historical data
                if (model.CurrentYear != goal.CurrentYear)
                {
                    var existingValue = goal.YearlyValues.FirstOrDefault(yv => yv.Year == goal.CurrentYear);
                    if (existingValue == null)
                    {
                        var historicalValue = new FrameworkGoalYearlyValue
                        {
                            FrameworkGoalID = goal.ID,
                            Year = goal.CurrentYear,
                            ActualValue = goal.BaseValueForCurrentYear,
                            DateRecorded = DateTime.Now
                        };
                        _context.FrameworkGoalYearlyValues.Add(historicalValue);
                    }
                }

                // Update current year and value
                goal.CurrentYear = model.CurrentYear;
                goal.BaseValueForCurrentYear = model.BaseValueForCurrentYear;

                // Update historical yearly values
                if (model.YearlyValues != null && model.YearlyValues.Any())
                {
                    foreach (var yearlyValue in model.YearlyValues)
                    {
                        // Skip current year (already handled above)
                        if (yearlyValue.Year == model.CurrentYear)
                        {
                            continue;
                        }

                        // Validate year is within range
                        if (yearlyValue.Year < goal.StartingYear || yearlyValue.Year >= goal.TargetYear)
                        {
                            continue; // Skip invalid years
                        }

                        // Special handling for Starting Year - update the main FrameworkGoal property
                        if (yearlyValue.Year == goal.StartingYear)
                        {
                            goal.BaseValueForStartingYear = yearlyValue.Value;
                            continue; // Don't store starting year in YearlyValues table
                        }

                        // For all other years (between starting and current), store in YearlyValues table
                        var existingYearlyValue = goal.YearlyValues.FirstOrDefault(yv => yv.Year == yearlyValue.Year);
                        if (existingYearlyValue != null)
                        {
                            // Update existing value
                            existingYearlyValue.ActualValue = yearlyValue.Value;
                            existingYearlyValue.DateRecorded = DateTime.Now;
                            _context.Update(existingYearlyValue);
                        }
                        else
                        {
                            // Add new value
                            var newYearlyValue = new FrameworkGoalYearlyValue
                            {
                                FrameworkGoalID = goal.ID,
                                Year = yearlyValue.Year,
                                ActualValue = yearlyValue.Value,
                                DateRecorded = DateTime.Now
                            };
                            _context.FrameworkGoalYearlyValues.Add(newYearlyValue);
                        }
                    }
                }

                _context.Update(goal);
                await _context.SaveChangesAsync();

                // Reload yearly values to get updated data
                await _context.Entry(goal).Collection(g => g.YearlyValues).LoadAsync();

                return Json(new
                {
                    success = true,
                    message = _localizer["Goal values updated successfully!"].Value,
                    goal = new
                    {
                        id = goal.ID,
                        startingYear = goal.StartingYear,
                        baseValueForStartingYear = goal.BaseValueForStartingYear,
                        currentYear = goal.CurrentYear,
                        baseValueForCurrentYear = goal.BaseValueForCurrentYear,
                        targetYear = goal.TargetYear,
                        targetValue = goal.TargetValue,
                        annualDiscountRate = goal.AnnualDiscountRate,
                        amountOfReduction = goal.AmountOfReduction,
                        expectedValue = goal.ExpectedValueForCurrentYear,
                        expectedTargetValueForCurrentYear = goal.ExpectedTargetValueForCurrentYear,
                        progressRate = goal.ProgressRate
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while updating the goal values."].Value });
            }
        }

        // GET: FrameworkGoals/GetYearlyValues - Get all historical yearly values for a goal
        [HttpGet]
        public async Task<IActionResult> GetYearlyValues(int goalId)
        {
            try
            {
                var goal = await _context.FrameworkGoals
                    .Include(g => g.YearlyValues)
                    .FirstOrDefaultAsync(g => g.ID == goalId);

                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(goalId))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to view this goal."].Value });
                }

                var yearlyValues = goal.YearlyValues
                    .OrderBy(yv => yv.Year)
                    .Select(yv => new
                    {
                        year = yv.Year,
                        value = yv.ActualValue,
                        dateRecorded = yv.DateRecorded.ToString("yyyy-MM-dd")
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    yearlyValues = yearlyValues,
                    startingYear = goal.StartingYear,
                    baseValueForStartingYear = goal.BaseValueForStartingYear,
                    currentYear = goal.CurrentYear,
                    targetYear = goal.TargetYear
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred."].Value });
            }
        }

        // POST: FrameworkGoals/SaveYearlyValue - Add or update a yearly value
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> SaveYearlyValue(int goalId, int year, double value)
        {
            try
            {
                var goal = await _context.FrameworkGoals
                    .Include(g => g.YearlyValues)
                    .FirstOrDefaultAsync(g => g.ID == goalId);

                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(goalId))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this goal."].Value });
                }

                // Validate year is within range
                if (year <= goal.StartingYear || year >= goal.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Year must be between starting year and target year."].Value });
                }

                // Check if value already exists for this year
                var existingValue = goal.YearlyValues.FirstOrDefault(yv => yv.Year == year);
                if (existingValue != null)
                {
                    // Update existing value
                    existingValue.ActualValue = value;
                    existingValue.DateRecorded = DateTime.Now;
                    _context.Update(existingValue);
                }
                else
                {
                    // Add new value
                    var yearlyValue = new FrameworkGoalYearlyValue
                    {
                        FrameworkGoalID = goalId,
                        Year = year,
                        ActualValue = value,
                        DateRecorded = DateTime.Now
                    };
                    _context.FrameworkGoalYearlyValues.Add(yearlyValue);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = _localizer["Yearly value saved successfully!"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while saving the value."].Value });
            }
        }

        // GET: FrameworkGoals/GetNotes - Get notes for a goal
        [HttpGet]
        public async Task<IActionResult> GetNotes(int goalId)
        {
            try
            {
                var goal = await _context.FrameworkGoals.FindAsync(goalId);
                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(goalId))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to view this goal."].Value });
                }

                return Json(new { success = true, notes = goal.Notes ?? "" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred."].Value });
            }
        }

        // POST: FrameworkGoals/SaveNotes - Save notes for a goal
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> SaveNotes(int goalId, string notes)
        {
            try
            {
                var goal = await _context.FrameworkGoals.FindAsync(goalId);
                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(goalId))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this goal."].Value });
                }

                goal.Notes = notes ?? "";
                _context.Update(goal);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = _localizer["Notes saved successfully!"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while saving notes."].Value });
            }
        }

        // GET: FrameworkGoals/GetAttachments - Get attachments for a goal
        [HttpGet]
        public async Task<IActionResult> GetAttachments(int goalId)
        {
            try
            {
                if (!await GoalBelongsToScopeAsync(goalId))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to view this goal."].Value });
                }

                var files = await _context.FrameworkGoalFiles
                    .Where(f => f.FrameworkGoalID == goalId)
                    .OrderByDescending(f => f.UploadedDate)
                    .Select(f => new { f.Id, f.FileName, f.UploadedDate })
                    .ToListAsync();

                // Return authorized action URLs, never the raw storage path - uploads are no
                // longer served statically.
                var attachments = files.Select(f => new
                {
                    id = f.Id,
                    fileName = f.FileName,
                    url = Url.Action("FrameworkGoal", "Files", new { id = f.Id }),
                    downloadUrl = Url.Action("FrameworkGoal", "Files", new { id = f.Id, download = true }),
                    uploadedDate = f.UploadedDate.ToString("yyyy-MM-dd HH:mm")
                });

                return Json(new { success = true, attachments = attachments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred."].Value });
            }
        }

        // POST: FrameworkGoals/UploadAttachment - Upload attachment for a goal
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> UploadAttachment(int goalId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = _localizer["Please select a file to upload."].Value });
                }

                var goal = await _context.FrameworkGoals.FindAsync(goalId);
                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(goalId))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this goal."].Value });
                }

                // Validate and store via the shared service. This previously built the path as
                // $"{Guid.NewGuid()}_{file.FileName}" with no Path.GetFileName and FileMode.Create,
                // so a filename like "../../web.config" escaped the uploads folder and
                // OVERWROTE arbitrary files. There was also no type, size or content check.
                var upload = await _uploadValidation.SaveAsync(file, UploadPurpose.Attachment, "frameworkgoals");
                if (!upload.Ok)
                {
                    return Json(new { success = false, message = upload.Error });
                }

                // Save file record to database. The original name is kept for display only and
                // is sanitized, because it is rendered in the attachments list.
                var attachment = new FrameworkGoalFile
                {
                    FrameworkGoalID = goalId,
                    FileName = _uploadValidation.SanitizeDisplayName(file.FileName),
                    FilePath = upload.RelativePath!,
                    UploadedDate = DateTime.Now
                };

                _context.FrameworkGoalFiles.Add(attachment);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = _localizer["File uploaded successfully!"].Value,
                    attachment = new
                    {
                        id = attachment.Id,
                        fileName = attachment.FileName,
                        url = Url.Action("FrameworkGoal", "Files", new { id = attachment.Id }),
                        downloadUrl = Url.Action("FrameworkGoal", "Files", new { id = attachment.Id, download = true }),
                        uploadedDate = attachment.UploadedDate.ToString("yyyy-MM-dd HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while uploading the file."].Value });
            }
        }

        // POST: FrameworkGoals/DeleteAttachment - Delete an attachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.ModifyStrategy)]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            try
            {
                var attachment = await _context.FrameworkGoalFiles.FindAsync(attachmentId);
                if (attachment == null)
                {
                    return Json(new { success = false, message = _localizer["Attachment not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(attachment.FrameworkGoalID))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to modify this goal."].Value });
                }

                // Delete physical file. Resolved through the upload service so the path is
                // containment-checked and both the current and legacy locations are handled.
                var filePath = _uploadValidation.ResolveStoredPath(attachment.FilePath);
                if (filePath != null && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Delete database record
                _context.FrameworkGoalFiles.Remove(attachment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = _localizer["Attachment deleted successfully!"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while deleting the attachment."].Value });
            }
        }

        // POST: FrameworkGoals/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.DeleteStrategy)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var goal = await _context.FrameworkGoals.FindAsync(id);
                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."].Value });
                }

                if (!await GoalBelongsToScopeAsync(id))
                {
                    return Json(new { success = false, message = _localizer["You are not authorized to delete this goal."].Value });
                }

                _context.FrameworkGoals.Remove(goal);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = _localizer["Goal deleted successfully!"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while deleting the goal."].Value });
            }
        }

        private bool FrameworkGoalExists(int id)
        {
            return _context.FrameworkGoals.Any(e => e.ID == id);
        }

        // GET: FrameworkGoals/ExportExcel
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> ExportExcel(int? frameworkCode)
        {
            var goals = await GetFilteredGoals(frameworkCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Framework Goals"].Value);

            // Set RTL for Arabic
            if (isRtl)
            {
                worksheet.RightToLeft = true;
            }

            // Header row
            worksheet.Cell(1, 1).Value = _localizer["Goal Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Framework"].Value;
            worksheet.Cell(1, 3).Value = _localizer["Goal Type"].Value;
            worksheet.Cell(1, 4).Value = _localizer["Unit of Measure"].Value;
            worksheet.Cell(1, 5).Value = _localizer["Starting Year"].Value;
            worksheet.Cell(1, 6).Value = _localizer["Base Value (Starting)"].Value;
            worksheet.Cell(1, 7).Value = _localizer["Current Year"].Value;
            worksheet.Cell(1, 8).Value = _localizer["Base Value (Current)"].Value;
            worksheet.Cell(1, 9).Value = _localizer["Target Year"].Value;
            worksheet.Cell(1, 10).Value = _localizer["Target Value"].Value;
            worksheet.Cell(1, 11).Value = _localizer["Progress Rate"].Value + " (%)";
            worksheet.Cell(1, 12).Value = _localizer["Status"].Value;

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 12);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int row = 2;
            foreach (var goal in goals)
            {
                // Derived from the single canonical FrameworkGoal.TrackingStatus, same as the on-screen table
                bool isOnTrack = goal.TrackingStatus == FrameworkGoalTrackingStatus.OnTrack;

                worksheet.Cell(row, 1).Value = goal.Name;
                worksheet.Cell(row, 2).Value = goal.Framework?.Name ?? "";
                worksheet.Cell(row, 3).Value = goal.IsQuantitative
                    ? _localizer["Quantitative"].Value
                    : _localizer["Qualitative"].Value;
                worksheet.Cell(row, 4).Value = goal.DisplayUnit;
                worksheet.Cell(row, 5).Value = goal.StartingYear;
                // Values stay numeric so the sheet remains computable; column 4 states the unit
                worksheet.Cell(row, 6).Value = goal.BaseValueForStartingYear;
                worksheet.Cell(row, 7).Value = goal.CurrentYear;
                worksheet.Cell(row, 8).Value = goal.BaseValueForCurrentYear;
                worksheet.Cell(row, 9).Value = goal.TargetYear;
                worksheet.Cell(row, 10).Value = goal.TargetValue;
                worksheet.Cell(row, 11).Value = Math.Round(goal.ProgressRate, 2);
                worksheet.Cell(row, 12).Value = isOnTrack ? _localizer["On Track"].Value : _localizer["Off Track"].Value;

                // Color coding for status
                worksheet.Cell(row, 12).Style.Font.FontColor = isOnTrack ? XLColor.Green : XLColor.Red;
                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add borders
            var dataRange = worksheet.Range(1, 1, row - 1, 12);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "أهداف_الإطار" : "FrameworkGoals";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // GET: FrameworkGoals/ExportPdf
        [HttpGet]
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> ExportPdf(int? frameworkCode)
        {
            var goals = await GetFilteredGoals(frameworkCode);
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(9));
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
                                col.Item().Text(_localizer["Framework Goals"].Value)
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
                                columns.RelativeColumn(3);  // Goal Name
                                columns.RelativeColumn(2);  // Framework
                                columns.RelativeColumn(1.2f);  // Unit ("%" => qualitative, otherwise quantitative)
                                columns.RelativeColumn(1);  // Starting Year
                                columns.RelativeColumn(1.5f);  // Base Starting
                                columns.RelativeColumn(1);  // Current Year
                                columns.RelativeColumn(1.5f);  // Base Current
                                columns.RelativeColumn(1);  // Target Year
                                columns.RelativeColumn(1.5f);  // Target Value
                                columns.RelativeColumn(1.2f);  // Progress
                                columns.RelativeColumn(1.2f);  // Status
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Goal Name"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Framework"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Unit"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Start"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Base Start"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Current"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Base Current"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Target"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Target Val"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Progress"].Value).FontColor(Colors.White).Bold().FontSize(8);
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5)
                                    .Text(_localizer["Status"].Value).FontColor(Colors.White).Bold().FontSize(8);
                            });

                            // Data rows
                            foreach (var goal in goals)
                            {
                                // Derived from the single canonical FrameworkGoal.TrackingStatus, same as the on-screen table
                                bool isOnTrack = goal.TrackingStatus == FrameworkGoalTrackingStatus.OnTrack;
                                var progressRate = Math.Round(goal.ProgressRate, 1);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.Name).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.Framework?.Name ?? "").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.DisplayUnit).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.StartingYear.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.BaseValueForStartingYear.ToString("N2")).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.CurrentYear.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.BaseValueForCurrentYear.ToString("N2")).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.TargetYear.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(goal.TargetValue.ToString("N2")).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text($"{progressRate}%")
                                    .FontColor(GetPerformanceColor(progressRate)).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(isOnTrack ? _localizer["On Track"].Value : _localizer["Off Track"].Value)
                                    .FontColor(isOnTrack ? Colors.Green.Darken2 : Colors.Red.Darken2).FontSize(8);
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
            var filePrefix = isRtl ? "أهداف_الإطار" : "FrameworkGoals";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<List<FrameworkGoal>> GetFilteredGoals(int? frameworkCode)
        {
            var query = await GetScopedGoalsQueryAsync();

            if (frameworkCode.HasValue)
            {
                query = query.Where(fg => fg.FrameworkCode == frameworkCode.Value);
            }

            return await query
                .Include(fg => fg.Framework)
                .OrderByDescending(fg => fg.ID)
                .ToListAsync();
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

    // Model for creating framework goals
    public class FrameworkGoalCreateModel
    {
        public string Name { get; set; } = string.Empty;
        public int StartingYear { get; set; }
        public string BaseValueForStartingYear { get; set; } = string.Empty;
        public int CurrentYear { get; set; }
        public string BaseValueForCurrentYear { get; set; } = string.Empty;
        public int TargetYear { get; set; }
        public string TargetValue { get; set; } = string.Empty;
        public int FrameworkCode { get; set; }

        // When true, expected target values for intermediate years are provided manually
        public bool ManualYearlyTargets { get; set; } = false;

        // Qualitative (default) = percentage values; Quantitative = quantities expressed in Unit
        public FrameworkGoalType GoalType { get; set; } = FrameworkGoalType.Qualitative;

        // Required when GoalType is Quantitative, ignored otherwise
        public string? Unit { get; set; }

        // Parallel lists for manual expected target years and their values
        public List<int> ManualTargetYears { get; set; } = new List<int>();
        public List<string> ManualTargetValues { get; set; } = new List<string>();

        // Parses a decimal value (handles both comma and period). Returns false — rather than
        // silently defaulting to 0 — when the value is missing or not a valid number, so a typo
        // rejects the request instead of quietly becoming a 0 target.
        public static bool TryParseDecimal(string? value, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var normalized = value.Replace(',', '.');
            return double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
        }
    }

    // Model for updating goal values (current and historical)
    public class UpdateGoalValuesModel
    {
        public int Id { get; set; }
        public int CurrentYear { get; set; }
        public double BaseValueForCurrentYear { get; set; }
        public List<YearlyValueModel>? YearlyValues { get; set; }
    }

    // Model for yearly value
    public class YearlyValueModel
    {
        public int Year { get; set; }
        public double Value { get; set; }
    }
}
