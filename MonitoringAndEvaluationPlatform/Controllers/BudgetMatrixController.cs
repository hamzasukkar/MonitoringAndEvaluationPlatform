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

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int? ministryCode)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
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

            var projects = await query.OrderBy(p => p.ProjectName).ToListAsync();
            var categories = ProjectPhase.DefaultCategoryNames.ToList();

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Budget Matrix");
                var row = 1;

                // Header
                worksheet.Cell(row, 1).Value = "Project Name";
                worksheet.Cell(row, 2).Value = "Type";
                for (int i = 0; i < categories.Count; i++)
                {
                    worksheet.Cell(row, 3 + i).Value = categories[i];
                }
                worksheet.Row(row).Style.Font.Bold = true;
                worksheet.Row(row).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(102, 126, 234);
                worksheet.Row(row).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

                row++;

                // Data
                foreach (var project in projects)
                {
                    var budgetRow = row;
                    worksheet.Cell(row, 1).Value = project.ProjectName;
                    worksheet.Cell(row, 2).Value = "Budget";
                    for (int i = 0; i < categories.Count; i++)
                    {
                        var phase = project.Phases?.FirstOrDefault(ph => ph.Name == categories[i]);
                        worksheet.Cell(row, 3 + i).Value = phase?.Budget ?? 0;
                    }
                    row++;

                    worksheet.Cell(row, 1).Value = project.ProjectName;
                    worksheet.Cell(row, 2).Value = "Disbursement";
                    for (int i = 0; i < categories.Count; i++)
                    {
                        var phase = project.Phases?.FirstOrDefault(ph => ph.Name == categories[i]);
                        var disb = phase?.ActionPlan?.Plans.Sum(pl => (double)pl.Realised) ?? 0;
                        worksheet.Cell(row, 3 + i).Value = disb;
                    }
                    row++;
                }

                // Totals
                worksheet.Cell(row, 1).Value = "Total";
                worksheet.Cell(row, 2).Value = "Budget";
                for (int i = 0; i < categories.Count; i++)
                {
                    var total = projects.Sum(p => p.Phases?.FirstOrDefault(ph => ph.Name == categories[i])?.Budget ?? 0);
                    worksheet.Cell(row, 3 + i).Value = total;
                }
                worksheet.Row(row).Style.Font.Bold = true;
                row++;

                worksheet.Cell(row, 1).Value = "Total";
                worksheet.Cell(row, 2).Value = "Disbursement";
                for (int i = 0; i < categories.Count; i++)
                {
                    var total = projects.Sum(p => p.Phases?.FirstOrDefault(ph => ph.Name == categories[i])?.ActionPlan?.Plans.Sum(pl => (double)pl.Realised) ?? 0);
                    worksheet.Cell(row, 3 + i).Value = total;
                }
                worksheet.Row(row).Style.Font.Bold = true;

                // Format columns
                for (int i = 3; i < 3 + categories.Count; i++)
                {
                    worksheet.Column(i).Width = 15;
                    worksheet.Column(i).Style.NumberFormat.Format = "#,##0";
                }
                worksheet.Column(1).Width = 25;
                worksheet.Column(2).Width = 12;

                var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"BudgetMatrix_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
        }
    }
}
