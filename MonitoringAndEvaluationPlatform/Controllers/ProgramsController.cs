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
    public class ProgramsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgramsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ActionPlan()
        {
            return View();
        }

        // GET: Programs
        public async Task<IActionResult> Index(ProgramFilterViewModel filter)
        {
            filter.Ministries = await _context.Ministrie.ToListAsync();
            filter.Regions = await _context.Region.ToListAsync();
            filter.Donors = await _context.Donor.ToListAsync();


            var programs = _context.Program.ToList();

            if (filter.SelectedMinistries.Any())
            {
                programs = programs.Where(p => filter.SelectedMinistries.Contains(p.MinistrieCode)).ToList();
            }

            if (filter.SelectedRegions.Any())
            {
                programs = programs.Where(p => filter.SelectedRegions.Contains(p.RegionCode)).ToList();
            }

            if (filter.SelectedDonors.Any())
            {
                programs = programs.Where(p => filter.SelectedDonors.Contains(p.DonorCode)).ToList();
            }

             filter.Programs =  programs.ToList();


            return View(filter);
        }



        // GET: Programs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.Program
                .FirstOrDefaultAsync(m => m.ProjectID == id);
            if (program == null)
            {
                return NotFound();
            }

            return View(program);
        }

        // GET: Programs/Create
        public IActionResult Create()
        {

            ViewData["Donor"] = new SelectList(_context.Donor, "Code", "Partner");
            ViewData["Region"] = new SelectList(_context.Region, "Code", "Name");
            ViewData["Ministrie"] = new SelectList(_context.Ministrie, "Code", "Partner");
            ViewData["SuperVisor"] = new SelectList(_context.SuperVisor, "Code", "Name");
            ViewData["ProjectManager"] = new SelectList(_context.ProjectManager, "Code", "Name");

            return View();
        }

        // POST: Programs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Models.Program program)
        {
            
                _context.Program.Add(program);
                _context.SaveChanges();
                return RedirectToAction("Index"); // Or your desired action
        }

        // GET: Programs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.Program.FindAsync(id);
            if (program == null)
            {
                return NotFound();
            }
            return View(program);
        }

        // POST: Programs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,ProjectName,EstimatedBudget,RealBudget,Trend,ProjectManager,SuperVisor,Type,Status1,Status2,Category,Donor,StartDate,EndDate,Region,performance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Models.Program program)
        {
            if (id != program.ProjectID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(program);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgramExists(program.ProjectID))
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
            return View(program);
        }

        // GET: Programs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.Program
                .FirstOrDefaultAsync(m => m.ProjectID == id);
            if (program == null)
            {
                return NotFound();
            }

            return View(program);
        }

        // POST: Programs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var program = await _context.Program.FindAsync(id);
            if (program != null)
            {
                _context.Program.Remove(program);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProgramExists(int id)
        {
            return _context.Program.Any(e => e.ProjectID == id);
        }
    }
}
