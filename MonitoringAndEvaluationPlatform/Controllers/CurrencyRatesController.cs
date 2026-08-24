using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// Maintains the platform-wide fallback exchange rates used to express cross-project
    /// budget totals in Syrian Pounds. Edit only — the supported currency set is fixed.
    /// </summary>
    [Authorize(Roles = UserRoles.SystemAdministrator)]
    public class CurrencyRatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyConversionService _conversionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<CurrencyRatesController> _localizer;

        public CurrencyRatesController(
            ApplicationDbContext context,
            ICurrencyConversionService conversionService,
            UserManager<ApplicationUser> userManager,
            IStringLocalizer<CurrencyRatesController> localizer)
        {
            _context = context;
            _conversionService = conversionService;
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var rates = await _context.CurrencyRates
                .AsNoTracking()
                .OrderBy(r => r.Code == CurrencyConverter.BaseCurrency ? 0 : 1)
                .ThenBy(r => r.Code)
                .ToListAsync();

            // Projects the converter cannot handle: an unknown/blank currency with no platform
            // rate to fall back on. Surfaced here so an admin can fix them at the source rather
            // than discovering a silently short total on a dashboard.
            var knownCodes = rates.Select(r => r.Code).ToList();
            ViewBag.UnconvertibleProjects = await _context.Projects
                .AsNoTracking()
                .Where(p => (p.ExchangeRate == null || p.ExchangeRate <= 0)
                            && (p.Currency == null || p.Currency == "" || !knownCodes.Contains(p.Currency)))
                .Select(p => new { p.ProjectID, p.ProjectName, p.Currency, p.EstimatedBudget })
                .OrderByDescending(p => p.EstimatedBudget)
                .Take(50)
                .ToListAsync();

            return View(rates);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var rate = await _context.CurrencyRates.FindAsync(id);
            if (rate == null) return NotFound();

            return View(rate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, decimal rateToSyp)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var rate = await _context.CurrencyRates.FindAsync(id);
            if (rate == null) return NotFound();

            // The base currency is the unit every other rate is expressed in; letting it drift
            // from 1 would silently rescale every total on the platform.
            if (string.Equals(rate.Code, CurrencyConverter.BaseCurrency, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(CurrencyRate.RateToSyp),
                    _localizer["The base currency rate is fixed at 1 and cannot be changed."]);
            }
            else if (rateToSyp <= 0)
            {
                ModelState.AddModelError(nameof(CurrencyRate.RateToSyp),
                    _localizer["The exchange rate must be greater than zero."]);
            }

            if (!ModelState.IsValid)
            {
                rate.RateToSyp = rateToSyp;
                return View(rate);
            }

            rate.RateToSyp = rateToSyp;
            rate.UpdatedAt = DateTime.UtcNow;
            rate.UpdatedBy = (await _userManager.GetUserAsync(User))?.UserName;

            await _context.SaveChangesAsync();
            _conversionService.InvalidateCache();

            this.SetSuccessMessage(string.Format(
                _localizer["Exchange rate for {0} has been updated."].Value, rate.Code));

            return RedirectToAction(nameof(Index));
        }
    }
}
