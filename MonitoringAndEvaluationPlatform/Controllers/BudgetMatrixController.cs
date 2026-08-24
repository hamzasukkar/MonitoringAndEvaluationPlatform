using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    // Experimental page: ministry projects budget/disbursement matrix by category ("الفقرات").
    [Authorize]
    public class BudgetMatrixController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrencyConversionService _currencyConversion;

        public BudgetMatrixController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICurrencyConversionService currencyConversion)
        {
            _context = context;
            _userManager = userManager;
            _currencyConversion = currencyConversion;
        }

        /// <summary>
        /// The matrix compares and totals figures across projects, so every cell is expressed in
        /// SYP rather than each project's own currency — a row-by-row mix would make the columns
        /// and the totals row meaningless.
        /// </summary>
        private static double ToSyp(double amount, Project project, CurrencyConverter converter) =>
            converter.ToSyp(amount, project.Currency, project.ExchangeRate) ?? 0;

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

            var conv = await _currencyConversion.GetConverterAsync();

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
                        row.Budget[category] = ToSyp(phase?.Budget ?? 0, p, conv);
                        row.Disbursement[category] = ToSyp(
                            phase?.ActionPlan?.Plans.Sum(pl => (double)pl.Realised) ?? 0, p, conv);
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
                var conv = await _currencyConversion.GetConverterAsync();
                var worksheet = workbook.Worksheets.Add("Budget Matrix");
                var row = 1;

                // Header
                worksheet.Cell(row, 1).Value = "Project Name (all amounts in SYP)";
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
                        worksheet.Cell(row, 3 + i).Value = ToSyp(phase?.Budget ?? 0, project, conv);
                    }
                    row++;

                    worksheet.Cell(row, 1).Value = project.ProjectName;
                    worksheet.Cell(row, 2).Value = "Disbursement";
                    for (int i = 0; i < categories.Count; i++)
                    {
                        var phase = project.Phases?.FirstOrDefault(ph => ph.Name == categories[i]);
                        var disb = ToSyp(phase?.ActionPlan?.Plans.Sum(pl => (double)pl.Realised) ?? 0, project, conv);
                        worksheet.Cell(row, 3 + i).Value = disb;
                    }
                    row++;
                }

                // Totals
                worksheet.Cell(row, 1).Value = "Total";
                worksheet.Cell(row, 2).Value = "Budget";
                for (int i = 0; i < categories.Count; i++)
                {
                    var total = projects.Sum(p => ToSyp(p.Phases?.FirstOrDefault(ph => ph.Name == categories[i])?.Budget ?? 0, p, conv));
                    worksheet.Cell(row, 3 + i).Value = total;
                }
                worksheet.Row(row).Style.Font.Bold = true;
                row++;

                worksheet.Cell(row, 1).Value = "Total";
                worksheet.Cell(row, 2).Value = "Disbursement";
                for (int i = 0; i < categories.Count; i++)
                {
                    var total = projects.Sum(p => ToSyp(p.Phases?.FirstOrDefault(ph => ph.Name == categories[i])?.ActionPlan?.Plans.Sum(pl => (double)pl.Realised) ?? 0, p, conv));
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
