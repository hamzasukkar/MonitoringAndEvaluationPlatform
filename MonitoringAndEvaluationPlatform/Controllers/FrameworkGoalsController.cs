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

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class FrameworkGoalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<HomeController> _localizer;

        public FrameworkGoalsController(ApplicationDbContext context, IStringLocalizer<HomeController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: FrameworkGoals
        [Permission(Permissions.ReadStrategies)]
        public async Task<IActionResult> Index(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework)
                .Include(fg => fg.YearlyValues);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/ProgressView
        public async Task<IActionResult> ProgressView(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/CircularGaugeView
        public async Task<IActionResult> CircularGaugeView(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/TimelineView
        public async Task<IActionResult> TimelineView(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/ValueProgressView
        public async Task<IActionResult> ValueProgressView(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/StatusBadgesView
        public async Task<IActionResult> StatusBadgesView(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
            return View(goals);
        }

        // GET: FrameworkGoals/MetricsGridView
        public async Task<IActionResult> MetricsGridView(int? frameworkCode)
        {
            ViewBag.Frameworks = await _context.Frameworks.ToListAsync();

            IQueryable<FrameworkGoal> goalsQuery = _context.FrameworkGoals
                .Include(fg => fg.Framework);

            if (frameworkCode.HasValue)
            {
                goalsQuery = goalsQuery.Where(fg => fg.FrameworkCode == frameworkCode.Value);
                ViewBag.SelectedFrameworkCode = frameworkCode.Value;
            }

            var goals = await goalsQuery.OrderByDescending(fg => fg.ID).ToListAsync();
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

                // Parse decimal values
                var baseValueStart = model.GetBaseValueForStartingYear();
                var baseValueCurrent = model.GetBaseValueForCurrentYear();
                var targetValue = model.GetTargetValue();

                // Validate framework exists
                var framework = await _context.Frameworks.FindAsync(model.FrameworkCode);
                if (framework == null)
                {
                    return Json(new { success = false, message = _localizer["Invalid framework selected."].Value });
                }

                // Validate year order
                if (model.StartingYear >= model.CurrentYear || model.CurrentYear >= model.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Years must be in ascending order: Starting Year < Current Year < Target Year."].Value });
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
                    FrameworkCode = model.FrameworkCode
                };

                _context.Add(frameworkGoal);
                await _context.SaveChangesAsync();

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
                        targetValue = frameworkGoal.TargetValue
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
                    return Json(new { success = false, message = _localizer["Goal not found."] });
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
                    message = _localizer["Current year updated successfully!"],
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
                return Json(new { success = false, message = _localizer["An error occurred while updating the goal."] });
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
                    return Json(new { success = false, message = _localizer["Goal not found."] });
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
                    message = _localizer["Goal values updated successfully!"],
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
                return Json(new { success = false, message = _localizer["An error occurred while updating the goal values."] });
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
                    return Json(new { success = false, message = _localizer["Goal not found."] });
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
                return Json(new { success = false, message = _localizer["An error occurred."] });
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
                    return Json(new { success = false, message = _localizer["Goal not found."] });
                }

                // Validate year is within range
                if (year <= goal.StartingYear || year >= goal.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Year must be between starting year and target year."] });
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

                return Json(new { success = true, message = _localizer["Yearly value saved successfully!"] });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while saving the value."] });
            }
        }

        // POST: FrameworkGoals/Delete
        [HttpPost]
        [Permission(Permissions.DeleteStrategy)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var goal = await _context.FrameworkGoals.FindAsync(id);
                if (goal == null)
                {
                    return Json(new { success = false, message = _localizer["Goal not found."] });
                }

                _context.FrameworkGoals.Remove(goal);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = _localizer["Goal deleted successfully!"] });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while deleting the goal."] });
            }
        }

        private bool FrameworkGoalExists(int id)
        {
            return _context.FrameworkGoals.Any(e => e.ID == id);
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

        // Helper method to parse decimal values (handles both comma and period)
        public double GetBaseValueForStartingYear() => ParseDecimal(BaseValueForStartingYear);
        public double GetBaseValueForCurrentYear() => ParseDecimal(BaseValueForCurrentYear);
        public double GetTargetValue() => ParseDecimal(TargetValue);

        private static double ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            // Replace comma with period and parse using invariant culture
            var normalized = value.Replace(',', '.');
            if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return 0;
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
