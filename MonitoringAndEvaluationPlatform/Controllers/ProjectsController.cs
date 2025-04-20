using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public async Task<IActionResult> ActionPlan()
        {
            return View();
        }

        // GET: Programs
        public async Task<IActionResult> Index(ProgramFilterViewModel filter)
        {
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Regions = await _context.Regions.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();


            var projects = _context.Projects.ToList();

            if (filter.SelectedMinistries.Any())
            {
                projects = projects.Where(p => filter.SelectedMinistries.Contains(p.MinistryCode)).ToList();
            }

            if (filter.SelectedRegions.Any())
            {
                projects = projects.Where(p => filter.SelectedRegions.Contains(p.RegionCode)).ToList();
            }

            if (filter.SelectedDonors.Any())
            {
                projects = projects.Where(p => filter.SelectedDonors.Contains(p.DonorCode)).ToList();
            }

             filter.Projects =  projects.ToList();


            return View(filter);
        }



        // GET: Programs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p=>p.ProjectManager)
                .Include(p=>p.SuperVisor)
                .Include(p=>p.Donor)
                .Include(p=>p.Region)
                .Include(p => p.Frameworks) // Include Frameworks here
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Programs/Create
        public IActionResult Create()
        {

            ViewData["Donor"] = new SelectList(_context.Donors, "Code", "Partner");
            ViewData["Region"] = new SelectList(_context.Regions, "Code", "Name");
            ViewData["Ministry"] = new SelectList(_context.Ministries, "Code", "MinistryName");
            ViewData["SuperVisor"] = new SelectList(_context.SuperVisors, "Code", "Name");
            ViewData["ProjectManager"] = new SelectList(_context.ProjectManagers, "Code", "Name");
            ViewBag.Indicators = _context.Indicators.ToList();

            var viewModel = new CreateProjectViewModel
            {
                AvailableFrameworks = _context.Frameworks.ToList()
            };

            return View(viewModel);
        }

        // POST: Programs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create( CreateProjectViewModel viewModel)
        {

            if (ModelState.IsValid)
            {
                var project = new Project
                {
                    ProjectName = viewModel.ProjectName,
                    RegionCode = viewModel.RegionCode,
                    ProjectManagerCode = viewModel.ProjectManagerCode,
                    SuperVisorCode = viewModel.SuperVisorCode,
                    MinistryCode = viewModel.MinistryCode,
                    DonorCode = viewModel.DonorCode,
                    Frameworks = _context.Frameworks
                        .Where(f => viewModel.SelectedFrameworkIds.Contains(f.Code))
                        .ToList()
                };

                _context.Projects.Add(project);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            // Repopulate frameworks if ModelState fails
            viewModel.AvailableFrameworks = _context.Frameworks.ToList();
            return View(viewModel);

        }

        // GET: Programs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.Projects.FindAsync(id);
            if (program == null)
            {
                return NotFound();
            }
            ViewBag.Indicators = _context.Indicators.ToList();
            return View(program);
        }

        // POST: Programs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,ProjectName,EstimatedBudget,RealBudget,Trend,ProjectManager,SuperVisor,Type,Status1,Status2,Category,Donor,StartDate,EndDate,Region,performance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Models.Project program)
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

            var program = await _context.Projects
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
            var program = await _context.Projects.FindAsync(id);
            if (program != null)
            {
                _context.Projects.Remove(program);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProgramExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectID == id);
        }
    }
}
