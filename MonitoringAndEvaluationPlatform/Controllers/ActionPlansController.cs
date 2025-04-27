using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ActionPlansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActionPlansController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ActionPlans
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ActionPlans.Include(a => a.Project);
            return View(await applicationDbContext.ToListAsync());
        }

        public async Task<IActionResult> Test()
        {
            return View();
        }

        public async Task<IActionResult> ActionPlan(int? id)
        {
            var actionPlan = await _context.ActionPlans.FirstOrDefaultAsync(ac => ac.ProjectID == id);

            if (actionPlan == null)
            {
                return RedirectToAction("Create"); // Redirect to Create action if no ActionPlan exists
            }

            var activities = await _context.Activities
                .Where(ac=>ac.ActionPlanCode== actionPlan.Code)
                .Include(a => a.Plans)
                .ToListAsync();

            if (activities.Count == 0)
            {
                return RedirectToAction("Create", "Activities"); // Redirect to Activities Create page
            }

            var groupedActivities = activities
                .GroupBy(a => a.ActivityType)
                .Select(g => new ActivityPlanViewModel
                {
                    ActivityType = g.Key.ToString(),
                    Activities = g.Select(a => new ActivityRow
                    {
                        ActivityName = a.Name,
                        Dates = a.Plans.Select(p => p.Date).ToList(),
                        PlannedValues = a.Plans.Select(p => p.Planned).ToList(),
                        RealisedValues = a.Plans.Select(p => p.Realised).ToList(),
                        TotalEstimatedCost = a.Plans.Sum(p => p.Planned), // Modify as needed
                        TotalRealisedCost = a.Plans.Sum(p => p.Realised) // Modify as needed
                    }).ToList()
                })
                .ToList();

            return View(groupedActivities);
        }

        // GET: ActionPlans/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actionPlan = await _context.ActionPlans
                .Include(a => a.Project)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (actionPlan == null)
            {
                return NotFound();
            }

            return View(actionPlan);
        }

        // GET: ActionPlans/Create
        public IActionResult Create()
        {
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName");
            return View();
        }

        // POST: ActionPlans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActionPlan actionPlan)
        {
            if (ModelState.IsValid || true)
            {
                _context.Add(actionPlan);
                await _context.SaveChangesAsync();
                return RedirectToRoute(new
                {
                    controller = "Activities",
                    action = "Create",
                    id = 5
                });
            }
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName", actionPlan.ProjectID);
            return View(actionPlan);
        }

        // GET: ActionPlans/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actionPlan = await _context.ActionPlans.FindAsync(id);
            if (actionPlan == null)
            {
                return NotFound();
            }
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName", actionPlan.ProjectID);
            return View(actionPlan);
        }

        // POST: ActionPlans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,PlansCount,ProjectName")] ActionPlan actionPlan)
        {
            if (id != actionPlan.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actionPlan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActionPlanExists(actionPlan.Code))
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
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectID", actionPlan.ProjectID);
            return View(actionPlan);
        }

        // GET: ActionPlans/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actionPlan = await _context.ActionPlans
                .Include(a => a.Project)
                .FirstOrDefaultAsync(m => m.Code == id);
            if (actionPlan == null)
            {
                return NotFound();
            }

            return View(actionPlan);
        }

        // POST: ActionPlans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actionPlan = await _context.ActionPlans.FindAsync(id);
            if (actionPlan != null)
            {
                _context.ActionPlans.Remove(actionPlan);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActionPlanExists(int id)
        {
            return _context.ActionPlans.Any(e => e.Code == id);
        }
    }
}
