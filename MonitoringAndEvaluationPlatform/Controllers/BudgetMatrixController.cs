using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    // Experimental page: ministry projects budget/disbursement matrix by category ("الفقرات").
    [Authorize]
    public class BudgetMatrixController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BudgetMatrixController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<(bool IsAdmin, int? MinistryCode)> GetScopeAsync()
        {
            if (User.IsInRole(UserRoles.SystemAdministrator))
            {
                return (true, null);
            }

            var user = await _userManager.GetUserAsync(User);
            return (false, user?.MinistryCode);
        }

        public async Task<IActionResult> Index(int? ministryCode)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();

            // Admins may filter by any ministry (null = all). Non-admins are always
            // forced to their own ministry, ignoring any passed ministryCode.
            int? effectiveMinistry = isAdmin ? ministryCode : scopedMinistryCode;

            var query = _context.Projects
                .Include(p => p.Phases)
                    .ThenInclude(ph => ph.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                .AsQueryable();

            if (isAdmin)
            {
                if (effectiveMinistry is not null)
                    query = query.Where(p => p.MinistryCode == effectiveMinistry);
            }
            else
            {
                query = effectiveMinistry is null
                    ? query.Where(_ => false)
                    : query.Where(p => p.MinistryCode == effectiveMinistry);
            }

            if (effectiveMinistry is not null)
            {
                var ministry = await _context.Ministries
                    .Where(m => m.Code == effectiveMinistry)
                    .Select(m => new { m.MinistryDisplayName_AR, m.MinistryDisplayName_EN })
                    .FirstOrDefaultAsync();

                if (ministry is not null)
                {
                    var isArabic = System.Globalization.CultureInfo.CurrentUICulture
                        .TwoLetterISOLanguageName == "ar";
                    ViewBag.MinistryName = isArabic
                        ? (ministry.MinistryDisplayName_AR ?? ministry.MinistryDisplayName_EN)
                        : (ministry.MinistryDisplayName_EN ?? ministry.MinistryDisplayName_AR);
                }
            }

            var projects = await query
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            var categories = ProjectPhase.DefaultCategoryNames.ToList();

            var model = new BudgetMatrixViewModel
            {
                Categories = categories,
                Projects = projects.Select(p =>
                {
                    var row = new BudgetMatrixRow
                    {
                        ProjectId = p.ProjectID,
                        ProjectName = p.ProjectName
                    };

                    foreach (var category in categories)
                    {
                        var phase = p.Phases?.FirstOrDefault(ph => ph.Name == category);
                        row.Budget[category] = phase?.Budget ?? 0;
                        row.Disbursement[category] = phase?.ActionPlan?.Plans
                            .Sum(pl => (double)pl.Realised) ?? 0;
                    }

                    return row;
                }).ToList()
            };

            return View(model);
        }
    }
}
