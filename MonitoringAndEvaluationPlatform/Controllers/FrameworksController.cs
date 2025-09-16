using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize] // Only Admins can access this controller
    public class FrameworksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<HomeController> _localizer;

        public FrameworksController(ApplicationDbContext context, IStringLocalizer<HomeController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

            return LocalRedirect(returnUrl ?? "/");
        }

        // GET: Frameworks
        public async Task<IActionResult> Index(string sortOrder, string searchString, FrameworkFilterViewModel filter)
        {
            ViewData["CurrentSort"] = sortOrder;
            // هنا قمنا بتغيير الفرز الافتراضي ليكون تنازليًا حسب أداء المؤشرات
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["IndicatorsSortParm"] = sortOrder == "indicators" ? "indicators_desc" : "indicators";
            ViewData["DisbursementSortParm"] = sortOrder == "disbursement" ? "disbursement_desc" : "disbursement";
            ViewData["CurrentFilter"] = searchString;

            // Load dropdown/filter data for the ViewModel
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();
            filter.Sectors = await _context.Sectors.ToListAsync();
            filter.IsMinistryUser = false; // Assuming this logic is handled elsewhere

            IQueryable<Framework> frameworksQuery = _context.Frameworks.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                frameworksQuery = frameworksQuery.Where(f => f.Name.Contains(searchString));
            }

            if (filter.SelectedMinistries != null && filter.SelectedMinistries.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Ministries.Any(min => filter.SelectedMinistries.Contains(min.Code))))))));
            }

            if (filter.SelectedDonors != null && filter.SelectedDonors.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Donors.Any(don => filter.SelectedDonors.Contains(don.Code))))))));
            }

            if (filter.SelectedSector != null && filter.SelectedSector.Any())
            {
                frameworksQuery = frameworksQuery.Where(f =>
                    f.Outcomes.Any(o =>
                        o.Outputs.Any(op =>
                            op.SubOutputs.Any(so =>
                                so.Indicators.Any(i =>
                                    i.ProjectIndicators.Any(pi =>
                                        pi.Project.Sectors.Any(sec => filter.SelectedSector.Contains(sec.Code))))))));
            }

            // Apply sorting logic
            switch (sortOrder)
            {
                case "name_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.Name);
                    break;
                case "indicators":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.IndicatorsPerformance);
                    break;
                case "indicators_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
                case "disbursement":
                    frameworksQuery = frameworksQuery.OrderBy(f => f.DisbursementPerformance);
                    break;
                case "disbursement_desc":
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.DisbursementPerformance);
                    break;
                default:
                    // تم تغيير القيمة الافتراضية هنا
                    frameworksQuery = frameworksQuery.OrderByDescending(f => f.IndicatorsPerformance);
                    break;
            }

            filter.Frameworks = await frameworksQuery.ToListAsync();
            return View(filter);
        }

        // GET: Frameworks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var framework = await _context.Frameworks
                .FirstOrDefaultAsync(m => m.Code == id);
            if (framework == null)
            {
                return NotFound();
            }

            return View(framework);
        }

        // GET: Frameworks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Frameworks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name")] Framework framework)
        {
            ModelState.Remove(nameof(framework.Outcomes));

            if (ModelState.IsValid)
            {
                _context.Add(framework);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(framework);
        }

        // POST: Frameworks/CreateInline - AJAX endpoint for inline creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Json(new { success = false, message = _localizer["Framework name is required."] });
                }

                // Check if framework name already exists
                var existingFramework = await _context.Frameworks.FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());
                if (existingFramework != null)
                {
                    return Json(new { success = false, message = _localizer["A framework with this name already exists."] });
                }

                // Create new framework
                var framework = new Framework
                {
                    Name = name.Trim(),
                    IndicatorsPerformance = 0,
                    DisbursementPerformance = 0,
                    FieldMonitoring = 0,
                    ImpactAssessment = 0
                };

                _context.Add(framework);
                await _context.SaveChangesAsync();

                // Return the created framework data for frontend update
                return Json(new
                {
                    success = true,
                    framework = new
                    {
                        code = framework.Code,
                        name = framework.Name,
                        indicatorsPerformance = Math.Round(framework.IndicatorsPerformance, 2),
                        disbursementPerformance = Math.Round(framework.DisbursementPerformance, 2)
                    },
                    message = _localizer["Framework created successfully!"]
                });
            }
            catch (Exception ex)
            {
                // Log the exception (you might want to use ILogger here)
                return Json(new { success = false, message = _localizer["An error occurred while creating the framework. Please try again."] });
            }
        }

        // GET: Frameworks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null)
            {
                return NotFound();
            }
            return View(framework);
        }

        // POST: Frameworks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Trend,IndicatorsPerformance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Framework framework)
        {
            ModelState.Remove(nameof(framework.Outcomes));

            if (id != framework.Code)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(framework);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FrameworkExists(framework.Code))
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
            return View(framework);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateName(int id, string name)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            framework.Name = name;
            _context.Update(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var framework = await _context.Frameworks.FindAsync(id);
            if (framework == null) return NotFound();

            _context.Frameworks.Remove(framework);
            await _context.SaveChangesAsync();

            return Ok();
        }



        // GET: Frameworks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var framework = await _context.Frameworks
                .FirstOrDefaultAsync(m => m.Code == id);
            if (framework == null)
            {
                return NotFound();
            }

            return View(framework);
        }

      
        private bool FrameworkExists(int id)
        {
            return _context.Frameworks.Any(e => e.Code == id);
        }

        public async Task<IActionResult>Monitoring()
        {
            return View(await _context.Frameworks.ToListAsync());
        }
    }
}
