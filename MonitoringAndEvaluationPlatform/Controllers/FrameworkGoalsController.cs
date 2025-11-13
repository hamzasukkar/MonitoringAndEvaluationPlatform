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
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = _localizer["Please fill in all required fields."] });
                }

                // Validate framework exists
                var framework = await _context.Frameworks.FindAsync(model.FrameworkCode);
                if (framework == null)
                {
                    return Json(new { success = false, message = _localizer["Invalid framework selected."] });
                }

                // Validate year order
                if (model.StartingYear >= model.CurrentYear || model.CurrentYear >= model.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Years must be in ascending order: Starting Year < Current Year < Target Year."] });
                }

                // Create new framework goal
                var frameworkGoal = new FrameworkGoal
                {
                    Name = model.Name.Trim(),
                    StartingYear = model.StartingYear,
                    BaseValueForStartingYear = model.BaseValueForStartingYear,
                    CurrentYear = model.CurrentYear,
                    BaseValueForCurrentYear = model.BaseValueForCurrentYear,
                    TargetYear = model.TargetYear,
                    TargetValue = model.TargetValue,
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
                    message = _localizer["Framework Goal created successfully!"]
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["An error occurred while creating the framework goal. Please try again."] });
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

                // Validate year order
                if (currentYear <= goal.StartingYear || currentYear >= goal.TargetYear)
                {
                    return Json(new { success = false, message = _localizer["Current year must be between starting year and target year."] });
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
        public double BaseValueForStartingYear { get; set; }
        public int CurrentYear { get; set; }
        public double BaseValueForCurrentYear { get; set; }
        public int TargetYear { get; set; }
        public double TargetValue { get; set; }
        public int FrameworkCode { get; set; }
    }
}
