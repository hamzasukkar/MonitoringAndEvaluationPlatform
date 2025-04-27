using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ActivityService _activityService;

        public ActivitiesController(ApplicationDbContext context)
        {
            _context = context;
            _activityService = new ActivityService(context);
        }

        // GET: Activities
        public async Task<IActionResult> Index()
        {
            var activities = await _context.Activities
                .Include(a => a.ActionPlan)
                .Include(a => a.Plans)
                .ToListAsync();

            var groupedActivities = activities
                .GroupBy(a => a.ActionPlan) // Group by ActionPlan
                .ToList();

            return View(groupedActivities);
        }


        // GET: Activities/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .Include(a => a.ActionPlan)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        // GET: Activities/Create
        public IActionResult Create()
        {
            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code");
            return View();
        }

        // POST: Activities/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            if (ModelState.IsValid || true)
            {
                bool success = await _activityService.CreateActivitiesForAllTypesAsync(activity);

                if (!success)
                {
                    ModelState.AddModelError("", "Invalid Action Plan.");
                    ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code", activity.ActionPlanCode);
                    return View(activity);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code", activity.ActionPlanCode);
            return View(activity);
        }


        // GET: Activities/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }
            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code", activity.ActionPlanCode);
            return View(activity);
        }

        // POST: Activities/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,ActivityType,ActionPlanCode")] Activity activity)
        {
            if (id != activity.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(activity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivityExists(activity.Code))
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
            ViewData["ActionPlanCode"] = new SelectList(_context.ActionPlans, "Code", "Code", activity.ActionPlanCode);
            return View(activity);
        }

        // GET: Activities/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .Include(a => a.ActionPlan)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (activity == null)
            {
                return NotFound();
            }

            return View(activity);
        }

        // POST: Activities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity != null)
            {
                _context.Activities.Remove(activity);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActivityExists(int id)
        {
            return _context.Activities.Any(e => e.Code == id);
        }
    }
}
