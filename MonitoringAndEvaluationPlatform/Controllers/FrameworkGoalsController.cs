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
                .Include(fg => fg.Framework);

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
