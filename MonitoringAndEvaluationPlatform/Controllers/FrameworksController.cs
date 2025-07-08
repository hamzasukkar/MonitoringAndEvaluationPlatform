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
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["ProgressBarClass"] = "progress-bar-danger";
            @ViewData["CurrentFilter"] = searchString;

            var framework = await _context.Frameworks.ToListAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                framework = framework.Where(f => f.Name.Contains(searchString)).ToList();
            }

            

                return View(framework);
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
