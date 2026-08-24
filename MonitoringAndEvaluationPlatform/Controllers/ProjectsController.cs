using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Helpers;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;
using Newtonsoft.Json;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly PlanService _planService;
        private readonly IProjectValidationService _validationService;
        private readonly IStringLocalizer<ProjectsController> _localizer;
        private readonly ICurrencyConversionService _currencyConversion;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, PlanService planService, IProjectValidationService validationService, IStringLocalizer<ProjectsController> localizer, ICurrencyConversionService currencyConversion)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _planService = planService;
            _validationService = validationService;
            _localizer = localizer;
            _currencyConversion = currencyConversion;
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

        /// <summary>
        /// Keeps the exchange rate consistent with the chosen currency before validation runs.
        /// A rate on an SYP project is meaningless (SYP converts 1:1), and a blank rate must not
        /// leave a stale date behind. The rate itself stays in ModelState so that
        /// <see cref="Attributes.RequiredWhenCurrencyNotSypAttribute"/> can reject a missing one.
        /// </summary>
        private void NormalizeExchangeRate(Project project)
        {
            var isBaseCurrency = string.Equals(
                project.Currency, CurrencyConverter.BaseCurrency, StringComparison.OrdinalIgnoreCase);

            if (isBaseCurrency || project.ExchangeRate is null or <= 0)
            {
                if (isBaseCurrency) project.ExchangeRate = null;
                project.ExchangeRateDate = null;
            }
            else if (project.ExchangeRateDate is null)
            {
                project.ExchangeRateDate = DateTime.Today;
            }

            ModelState.Remove(nameof(Project.ExchangeRateDate));
        }

        private async Task<int?> GetLinkedProgramMinistryCodeAsync(int? indicatorId)
        {
            if (!indicatorId.HasValue) return null;

            return await _context.Indicators
                .Where(i => i.IndicatorCode == indicatorId.Value)
                .Select(i => (int?)i.SubOutput.Output.Outcome.Framework.MinistryCode)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> ProjectBelongsToScopeAsync(int projectId)
        {
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (isAdmin) return true;
            if (scopedMinistryCode is null) return false;

            return await _context.Projects
                .Where(p => p.ProjectID == projectId)
                .AnyAsync(p => p.MinistryCode == scopedMinistryCode);
        }


        public async Task<IActionResult> ActionPlan()
        {
            return View();
        }

        // GET: Programs
        [Permission(Permissions.ReadProjects)]
        public async Task<IActionResult> Index(ProgramFilterViewModel filter)
        {
            // Load dropdown/filter data
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();
            filter.Sectors = await _context.Sectors.ToListAsync();
            filter.PublicSectorTypes = await _context.PublicSectorTypes.ToListAsync();
            filter.Governorates = await _context.Governorates.ToListAsync();

            // Narrow the Framework dropdown to frameworks that actually have a project
            // under the selected ministry/ministries - same "Project.Ministries" definition
            // the SelectedMinistries project filter itself uses below, for consistency.
            filter.Frameworks = filter.SelectedMinistries.Any()
                ? await _context.Frameworks
                    .Where(f => f.Outcomes.Any(o => o.Outputs.Any(op => op.SubOutputs.Any(so =>
                        so.Indicators.Any(i => i.Project != null &&
                            i.Project.Ministries.Any(m => filter.SelectedMinistries.Contains(m.Code)))))))
                    .OrderBy(f => f.Name)
                    .ToListAsync()
                : await _context.Frameworks.OrderBy(f => f.Name).ToListAsync();

            // If the currently selected Framework (and anything cascading from it) no longer
            // belongs to the narrowed list, clear it - otherwise the dropdown silently shows
            // "All Frameworks" while still filtering by a hidden stale code.
            if (filter.SelectedFrameworkCode.HasValue &&
                !filter.Frameworks.Any(f => f.Code == filter.SelectedFrameworkCode.Value))
            {
                filter.SelectedFrameworkCode = null;
                filter.SelectedOutcomeCode = null;
                filter.SelectedOutputCode = null;
                filter.SelectedSubOutputCode = null;
            }

            // Build the filtered query (shared with ExportExcel)
            var projectQuery = await BuildFilteredProjectsQueryAsync(filter);

            // Aggregates over the FULL filtered set, before paging - the summary cards
            // must reflect every matching project, not just the current page.
            filter.TotalProjects = await projectQuery.CountAsync();
            filter.NotStartedProjects = await projectQuery.CountAsync(p => p.performance == 0);
            filter.InProgressProjects = await projectQuery.CountAsync(p => p.performance > 0 && p.performance < 100);
            filter.CompletedProjects = await projectQuery.CountAsync(p => p.performance >= 100);
            filter.TotalBudget = await _currencyConversion.SumBudgetToSypAsync(projectQuery);

            filter.TotalRecords = filter.TotalProjects;
            if (filter.CurrentPage < 1) filter.CurrentPage = 1;
            if (filter.PageSize < 5 || filter.PageSize > 100) filter.PageSize = 20;
            filter.TotalPages = (int)Math.Ceiling(filter.TotalRecords / (double)filter.PageSize);
            filter.CurrentPage = Math.Min(filter.CurrentPage, Math.Max(filter.TotalPages, 1));

            bool ascending = filter.SortDirection == "asc";
            projectQuery = filter.SortColumn?.ToLower() switch
            {
                "disbursement" => ascending
                    ? projectQuery.OrderBy(p => p.DisbursementPerformance).ThenBy(p => p.ProjectID)
                    : projectQuery.OrderByDescending(p => p.DisbursementPerformance).ThenBy(p => p.ProjectID),
                "lastmodified" => ascending
                    ? projectQuery.OrderBy(p => p.LastModifiedAt).ThenBy(p => p.ProjectID)
                    : projectQuery.OrderByDescending(p => p.LastModifiedAt).ThenBy(p => p.ProjectID),
                _ => ascending
                    ? projectQuery.OrderBy(p => p.performance).ThenBy(p => p.ProjectID)
                    : projectQuery.OrderByDescending(p => p.performance).ThenBy(p => p.ProjectID),
            };

            // Finalize and assign the current page's results (eager-load Ministries and
            // the Indicator -> SubOutput -> Output -> Outcome -> Framework chain so the
            // view can show each project's ministries and frameworks)
            filter.Projects = await projectQuery
                .Skip((filter.CurrentPage - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Include(p => p.Ministries)
                .Include(p => p.Indicators)
                    .ThenInclude(i => i.SubOutput)
                        .ThenInclude(so => so.Output)
                            .ThenInclude(o => o.Outcome)
                                .ThenInclude(oc => oc.Framework)
                .ToListAsync();

            return View(filter);
        }

        // Builds the filtered project query shared by Index and ExportExcel.
        // Mutates the supplied filter to set IsMinistryUser and the hierarchy display names.
        private async Task<IQueryable<Project>> BuildFilteredProjectsQueryAsync(ProgramFilterViewModel filter)
        {
            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);

            // Start with base project query
            var projectQuery = _context.Projects.AsQueryable();

            // If the user is associated with a Ministry (and not SystemAdministrator), filter projects to only that Ministry
            if (user?.MinistryName != null && !User.IsInRole(UserRoles.SystemAdministrator))
            {
                projectQuery = projectQuery
                    .Where(p => p.Ministries
                                 .Any(m => m.MinistryDisplayName_AR == user.MinistryName || m.MinistryDisplayName_EN == user.MinistryName || m.MinistryUserName == user.MinistryName));
                filter.IsMinistryUser = true;
            }

            // Restrict by MinistryCode for any non-admin (authoritative scoping)
            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin)
            {
                projectQuery = scopedMinistryCode is null
                    ? projectQuery.Where(_ => false)
                    : projectQuery.Where(p => p.MinistryCode == scopedMinistryCode);
                filter.IsMinistryUser = true;
            }

            // Apply SubOutput filter if provided (legacy URL parameter)
            if (filter.SubOutputCode.HasValue)
            {
                // Get the SubOutput name for display
                var subOutput = await _context.SubOutputs.FindAsync(filter.SubOutputCode.Value);
                if (subOutput != null)
                {
                    filter.SubOutputName = subOutput.Name;
                }

                // Filter projects that have indicators belonging to the specified SubOutput
                projectQuery = projectQuery
                    .Where(p => p.Indicators
                                 .Any(i => i.SubOutputCode == filter.SubOutputCode.Value));
            }

            // Apply hierarchy filters (Framework -> Outcome -> Output -> SubOutput)
            if (filter.SelectedSubOutputCode.HasValue)
            {
                // SubOutput filter is most specific
                projectQuery = projectQuery
                    .Where(p => p.Indicators
                                 .Any(i => i.SubOutputCode == filter.SelectedSubOutputCode.Value));

                // Get display names for breadcrumb
                var subOutput = await _context.SubOutputs
                    .Include(so => so.Output)
                        .ThenInclude(o => o.Outcome)
                            .ThenInclude(oc => oc.Framework)
                    .FirstOrDefaultAsync(so => so.Code == filter.SelectedSubOutputCode.Value);
                if (subOutput != null)
                {
                    filter.SubOutputName = subOutput.Name;
                    filter.OutputName = subOutput.Output?.Name;
                    filter.OutcomeName = subOutput.Output?.Outcome?.Name;
                    filter.FrameworkName = subOutput.Output?.Outcome?.Framework?.Name;
                }
            }
            else if (filter.SelectedOutputCode.HasValue)
            {
                // Output filter - get all SubOutputs under this Output
                var subOutputCodes = await _context.SubOutputs
                    .Where(so => so.OutputCode == filter.SelectedOutputCode.Value)
                    .Select(so => so.Code)
                    .ToListAsync();

                projectQuery = projectQuery
                    .Where(p => p.Indicators
                                 .Any(i => subOutputCodes.Contains(i.SubOutputCode)));

                // Get display names
                var output = await _context.Outputs
                    .Include(o => o.Outcome)
                        .ThenInclude(oc => oc.Framework)
                    .FirstOrDefaultAsync(o => o.Code == filter.SelectedOutputCode.Value);
                if (output != null)
                {
                    filter.OutputName = output.Name;
                    filter.OutcomeName = output.Outcome?.Name;
                    filter.FrameworkName = output.Outcome?.Framework?.Name;
                }
            }
            else if (filter.SelectedOutcomeCode.HasValue)
            {
                // Outcome filter - get all SubOutputs under Outputs under this Outcome
                var subOutputCodes = await _context.SubOutputs
                    .Where(so => so.Output.OutcomeCode == filter.SelectedOutcomeCode.Value)
                    .Select(so => so.Code)
                    .ToListAsync();

                projectQuery = projectQuery
                    .Where(p => p.Indicators
                                 .Any(i => subOutputCodes.Contains(i.SubOutputCode)));

                // Get display names
                var outcome = await _context.Outcomes
                    .Include(oc => oc.Framework)
                    .FirstOrDefaultAsync(oc => oc.Code == filter.SelectedOutcomeCode.Value);
                if (outcome != null)
                {
                    filter.OutcomeName = outcome.Name;
                    filter.FrameworkName = outcome.Framework?.Name;
                }
            }
            else if (filter.SelectedFrameworkCode.HasValue)
            {
                // Framework filter - get all SubOutputs under this Framework
                var subOutputCodes = await _context.SubOutputs
                    .Where(so => so.Output.Outcome.FrameworkCode == filter.SelectedFrameworkCode.Value)
                    .Select(so => so.Code)
                    .ToListAsync();

                projectQuery = projectQuery
                    .Where(p => p.Indicators
                                 .Any(i => subOutputCodes.Contains(i.SubOutputCode)));

                // Get display name
                var framework = await _context.Frameworks.FindAsync(filter.SelectedFrameworkCode.Value);
                if (framework != null)
                {
                    filter.FrameworkName = framework.Name;
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
            {
                var searchTerm = filter.SearchQuery.Trim().ToLower();

                // Get matching indicator codes based on hierarchy search
                var matchingIndicatorCodes = await _context.Indicators
                    .Include(i => i.SubOutput)
                        .ThenInclude(so => so.Output)
                            .ThenInclude(o => o.Outcome)
                                .ThenInclude(oc => oc.Framework)
                    .Where(i =>
                        i.Name.ToLower().Contains(searchTerm) ||
                        i.SubOutput.Name.ToLower().Contains(searchTerm) ||
                        i.SubOutput.Output.Name.ToLower().Contains(searchTerm) ||
                        i.SubOutput.Output.Outcome.Name.ToLower().Contains(searchTerm) ||
                        i.SubOutput.Output.Outcome.Framework.Name.ToLower().Contains(searchTerm))
                    .Select(i => i.IndicatorCode)
                    .ToListAsync();

                // Filter projects by name OR by matching indicators
                projectQuery = projectQuery
                    .Where(p => p.ProjectName.ToLower().Contains(searchTerm) ||
                                p.Indicators.Any(i => matchingIndicatorCodes.Contains(i.IndicatorCode)));
            }

            // Apply additional filters
            if (filter.SelectedMinistries.Any())
            {
                projectQuery = projectQuery
                    .Where(p => p.Ministries
                                 .Any(m => filter.SelectedMinistries.Contains(m.Code)));
            }

            if (filter.SelectedSectors.Any())
            {
                projectQuery = projectQuery
                    .Where(p => filter.SelectedSectors.Contains(p.SectorCode));
            }

            if (filter.SelectedPublicSectorTypes.Any())
            {
                projectQuery = projectQuery
                    .Where(p => p.PublicSectorTypeCode.HasValue &&
                                filter.SelectedPublicSectorTypes.Contains(p.PublicSectorTypeCode.Value));
            }

            if (filter.SelectedDonors.Any())
            {
                projectQuery = projectQuery
                    .Where(p => p.Donors
                                 .Any(d => filter.SelectedDonors.Contains(d.Code)));
            }

            if (filter.SelectedGovernorates.Any())
            {
                projectQuery = projectQuery
                    .Where(p => p.Governorates
                                 .Any(g => filter.SelectedGovernorates.Contains(g.Code)));
            }

            // Apply date filters
            if (filter.StartDateFrom.HasValue)
            {
                projectQuery = projectQuery.Where(p => p.StartDate >= filter.StartDateFrom.Value);
            }
            if (filter.StartDateTo.HasValue)
            {
                projectQuery = projectQuery.Where(p => p.StartDate <= filter.StartDateTo.Value);
            }
            if (filter.EndDateFrom.HasValue)
            {
                projectQuery = projectQuery.Where(p => p.EndDate >= filter.EndDateFrom.Value);
            }
            if (filter.EndDateTo.HasValue)
            {
                projectQuery = projectQuery.Where(p => p.EndDate <= filter.EndDateTo.Value);
            }

            return projectQuery;
        }

        // GET: Projects/ExportExcel — exports the currently filtered project list to Excel.
        [HttpGet]
        [Permission(Permissions.ReadProjects)]
        public async Task<IActionResult> ExportExcel(ProgramFilterViewModel filter)
        {
            var projectQuery = await BuildFilteredProjectsQueryAsync(filter);

            var projects = await projectQuery
                .Include(p => p.Ministries)
                .Include(p => p.Indicators)
                    .ThenInclude(i => i.SubOutput)
                        .ThenInclude(so => so.Output)
                            .ThenInclude(o => o.Outcome)
                                .ThenInclude(oc => oc.Framework)
                .ToListAsync();

            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
            var isRtl = culture.StartsWith("ar");

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(_localizer["Projects"].Value);

            if (isRtl)
            {
                worksheet.RightToLeft = true;
            }

            // Header row
            worksheet.Cell(1, 1).Value = _localizer["Project Name"].Value;
            worksheet.Cell(1, 2).Value = _localizer["Ministry"].Value;
            worksheet.Cell(1, 3).Value = _localizer["Framework"].Value;
            worksheet.Cell(1, 4).Value = _localizer["Start Date"].Value;
            worksheet.Cell(1, 5).Value = _localizer["End Date"].Value;
            worksheet.Cell(1, 6).Value = _localizer["Status"].Value;
            worksheet.Cell(1, 7).Value = _localizer["Performance"].Value + " (%)";
            worksheet.Cell(1, 8).Value = _localizer["Disbursement"].Value + " (%)";
            worksheet.Cell(1, 9).Value = _localizer["Estimated Budget"].Value;
            worksheet.Cell(1, 10).Value = _localizer["Currency"].Value;

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Data rows
            int row = 2;
            foreach (var project in projects)
            {
                var ministries = string.Join(", ", (project.Ministries ?? new List<Ministry>())
                    .Select(m => isRtl ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct());

                var frameworks = string.Join(", ", (project.Indicators ?? new List<Indicator>())
                    .Select(i => i.SubOutput?.Output?.Outcome?.Framework?.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct());

                var performance = Math.Round(project.performance, 2);
                string status = performance == 0
                    ? _localizer["Not Started"].Value
                    : performance < 100 ? _localizer["In Progress"].Value : _localizer["Completed"].Value;

                worksheet.Cell(row, 1).Value = project.ProjectName;
                worksheet.Cell(row, 2).Value = ministries;
                worksheet.Cell(row, 3).Value = frameworks;
                worksheet.Cell(row, 4).Value = project.StartDate;
                worksheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-MM-dd";
                worksheet.Cell(row, 5).Value = project.EndDate;
                worksheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-MM-dd";
                worksheet.Cell(row, 6).Value = status;
                worksheet.Cell(row, 7).Value = performance;
                worksheet.Cell(row, 8).Value = Math.Round(project.DisbursementPerformance, 2);
                worksheet.Cell(row, 9).Value = project.EstimatedBudget;
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
                // Each row keeps its own currency; the amounts are not converted here.
                worksheet.Cell(row, 10).Value = project.Currency;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            var dataRange = worksheet.Range(1, 1, Math.Max(row - 1, 1), 10);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var filePrefix = isRtl ? "المشاريع" : "Projects";
            var fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // APIs for cascading
        public JsonResult GetDistricts(string governorateCode)
        {
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var districts = _context.Districts
                .Where(d => d.GovernorateCode == governorateCode)
                .Select(d => new {
                    code = d.Code,
                    name = currentCulture == "ar" ? d.AR_Name : d.EN_Name
                })
                .ToList();
            return Json(districts);
        }

        public JsonResult GetSubDistricts(string districtCode)
        {
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var subs = _context.SubDistricts
                .Where(s => s.DistrictCode == districtCode)
                .Select(s => new {
                    code = s.Code,
                    name = currentCulture == "ar" ? s.AR_Name : s.EN_Name
                })
                .ToList();
            return Json(subs);
        }

        public JsonResult GetCommunities(string subDistrictCode)
        {
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var comms = _context.Communities
                .Where(c => c.SubDistrictCode == subDistrictCode)
                .Select(c => new {
                    code = c.Code,
                    name = currentCulture == "ar" ? c.AR_Name : c.EN_Name
                })
                .ToList();
            return Json(comms);
        }

        // Resolves the same-named District → SubDistrict → Community chain of a governorate,
        // used by the "Add Entire Governorate" button. Matching is on AR_Name (the canonical name).
        public async Task<JsonResult> GetGovernorateChain(string governorateCode)
        {
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var gov = await _context.Governorates.FindAsync(governorateCode);
            if (gov == null) return Json(new { found = false });

            var district = await _context.Districts
                .FirstOrDefaultAsync(d => d.GovernorateCode == gov.Code && d.AR_Name == gov.AR_Name);
            var subDistrict = district == null ? null : await _context.SubDistricts
                .FirstOrDefaultAsync(s => s.DistrictCode == district.Code && s.AR_Name == gov.AR_Name);
            var community = subDistrict == null ? null : await _context.Communities
                .FirstOrDefaultAsync(c => c.SubDistrictCode == subDistrict.Code && c.AR_Name == gov.AR_Name);

            if (district == null || subDistrict == null || community == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                governorate = new { code = gov.Code, name = currentCulture == "ar" ? gov.AR_Name : gov.EN_Name },
                district = new { code = district.Code, name = currentCulture == "ar" ? district.AR_Name : district.EN_Name },
                subDistrict = new { code = subDistrict.Code, name = currentCulture == "ar" ? subDistrict.AR_Name : subDistrict.EN_Name },
                community = new { code = community.Code, name = currentCulture == "ar" ? community.AR_Name : community.EN_Name }
            });
        }

        // GET: Programs/Create
        [Permission(Permissions.AddProject)]
        public async Task<IActionResult> Create(int? indicatorId, string indicatorName)
        {
            // Retrieve related data
            var donors = _context.Donors.ToList()
                .OrderBy(d => d.IsInvestmentBudget ? 0 : 1)
                .ThenBy(d => d.Partner)
                .ToList();
            var sectors = _context.Sectors.ToList();
            var ministries = _context.Ministries.ToList();
            var supervisors = _context.SuperVisors.ToList();
            var projectManagers = _context.ProjectManagers.ToList();
            var goals = _context.Goals.ToList();
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            ViewBag.Governorates = _context.Governorates.ToList();

            // Pass indicator information for auto-filling if coming from "Add & Create Project"
            ViewBag.PreSelectedIndicatorId = indicatorId;
            ViewBag.PreFilledProjectName = indicatorName;

            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);
            int? userMinistryCode = null;
            bool isMinistryUser = false;

            // Check if the user is associated with a Ministry (and not SystemAdministrator)
            if (user?.MinistryName != null && !User.IsInRole(UserRoles.SystemAdministrator))
            {
                var userMinistry = ministries.FirstOrDefault(m => m.MinistryDisplayName_AR == user.MinistryName || m.MinistryDisplayName_EN == user.MinistryName || m.MinistryUserName == user.MinistryName);
                if (userMinistry != null)
                {
                    userMinistryCode = userMinistry.Code;
                    isMinistryUser = true;
                }
            }

            // If this project is being created from a linked indicator whose program (SubOutput →
            // Output → Outcome → Framework) belongs to a ministry, that ministry wins over the
            // account's own ministry and over an admin's free choice.
            var linkedProgramMinistryCode = await GetLinkedProgramMinistryCodeAsync(indicatorId);
            bool isMinistryLockedByProgram = linkedProgramMinistryCode.HasValue;
            if (isMinistryLockedByProgram)
            {
                userMinistryCode = linkedProgramMinistryCode;
            }

            // Initialize project with defaults
            var project = new Project
            {
                EstimatedBudget = 0,
                RealBudget = 0,
                Currency = "SYP",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                //DonorCode = donors.FirstOrDefault()?.Code ?? 0,//To Check
                MinistryCode = userMinistryCode, // Set ministry for ministry users
                SuperVisorCode = supervisors.FirstOrDefault()?.Code ?? 0,
                ProjectManagerCode = projectManagers.FirstOrDefault()?.Code ?? 0,
                SectorCode = sectors.FirstOrDefault()?.Code ?? 0,
                GoalCode = goals.FirstOrDefault()?.Code ?? 0,
            };

            // Pre-fill project name if coming from indicator creation
            if (!string.IsNullOrEmpty(indicatorName))
            {
                project.ProjectName = indicatorName;
            }

            // Prepare dropdown and multiselect data
            ViewBag.Donor = new SelectList(donors, "Code", "Partner");
            ViewBag.InvestmentBudgetDonorCodes = donors
                .Where(d => d.IsInvestmentBudget)
                .Select(d => d.Code.ToString())
                .ToList();
            ViewBag.SectorList = new SelectList(sectors, "Code", isArabic ? "AR_Name" : "EN_Name", project.SectorCode);
            ViewBag.PublicSectorTypeList = new SelectList(_context.PublicSectorTypes.ToList(), "Code", isArabic ? "AR_Name" : "EN_Name");
            ViewBag.MinistryList = new SelectList(ministries, "Code", isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN", userMinistryCode);
            ViewBag.Ministries = ministries; // Pass full ministry list with Logo property
            ViewBag.PlatformRates = await _currencyConversion.GetFallbackRatesAsync();
            ViewBag.SuperVisor = new SelectList(supervisors, "Code", "Name");

            // Pass ministry user info to the view
            ViewBag.IsMinistryUser = isMinistryUser;
            ViewBag.UserMinistryCode = userMinistryCode;
            ViewBag.IsMinistryLockedByProgram = isMinistryLockedByProgram;

            // Initialize empty donor funding data for create form
            ViewBag.DonorFundingData = JsonConvert.SerializeObject(new Dictionary<string, decimal>());
            ViewBag.ProjectManager = new SelectList(projectManagers, "Code", "Name");
            ViewBag.Goals = new SelectList(
                goals,
                "Code",
                isArabic ? "AR_Name" : "EN_Name"
            );

            return View(project);
        }


        [HttpPost]
        [Permission(Permissions.AddProject)]
        public async Task<IActionResult> Create(
            Project project,
            List<IFormFile> UploadedFiles,
            string? selections,
            string? DonorFundingBreakdown,
            int? LinkedIndicatorId)
        {
            int? linkedProgramMinistryCode = null;
            try
            {
                // If this project is being created from a linked indicator whose program belongs to
                // a ministry, that ministry wins over the account's own ministry / an admin's choice.
                linkedProgramMinistryCode = await GetLinkedProgramMinistryCodeAsync(LinkedIndicatorId);

                // Remove navigation properties from model state FIRST, before any validation
                RemoveNavigationPropertiesFromModelState();

                // Remove form parameters from ModelState (they're not part of the Project model)
                ModelState.Remove("selections");
                ModelState.Remove("DonorFundingBreakdown");
                ModelState.Remove("IsEntireCountry");
                ModelState.Remove("SelectedPhases");

                // Explicitly read IsEntireCountry from form (checkbox sends "true" if checked, nothing if unchecked)
                var isEntireCountryValue = Request.Form["IsEntireCountry"].ToString();
                bool IsEntireCountry = isEntireCountryValue.Contains("true", StringComparison.OrdinalIgnoreCase);

                // Set IsEntireCountry on project
                project.IsEntireCountry = IsEntireCountry;

                // Process location selections (skip if entire country)
                if (!IsEntireCountry)
                {
                    await ProcessProjectLocationsAsync(project, selections);
                }

                // Get form data
                var selectedDonorCodes = Request.Form["Donors"].ToList();
                var selectedLocations = string.IsNullOrEmpty(selections)
                    ? new List<LocationSelectionViewModel>()
                    : JsonConvert.DeserializeObject<List<LocationSelectionViewModel>>(selections);

                // Check if project name already exists
                if (!string.IsNullOrWhiteSpace(project.ProjectName))
                {
                    var existingProject = await _context.Projects
                        .FirstOrDefaultAsync(p => p.ProjectName.ToLower() == project.ProjectName.Trim().ToLower());
                    if (existingProject != null)
                    {
                        ModelState.AddModelError("ProjectName", _localizer["A project with this name already exists."]);
                    }
                }

                // Check if any selected donor is "موازنة أستثمارية"
                bool hasInvestmentBudgetDonor = selectedDonorCodes.Any() && await _context.Donors
                    .AnyAsync(d => selectedDonorCodes.Contains(d.Code.ToString()) && d.IsInvestmentBudget);

                // Read selected phases; required only when donor is "موازنة أستثمارية"; otherwise one phase is auto-created
                var selectedPhases = Request.Form["SelectedPhases"].ToList();
                if (hasInvestmentBudgetDonor && !selectedPhases.Any())
                    ModelState.AddModelError("SelectedPhases", _localizer["Please select at least one phase."]);

                // Validate project creation
                _validationService.ValidateProjectCreation(
                    project,
                    selectedLocations,
                    ModelState,
                    IsEntireCountry);

                // Sector type is required for every sector now
                ValidateSectorType(project, requireSectorType: true);

                NormalizeExchangeRate(project);

                if (linkedProgramMinistryCode.HasValue)
                {
                    // Program's ministry wins — even over an admin's choice or the account's own ministry.
                    project.MinistryCode = linkedProgramMinistryCode;
                }
                else
                {
                    // Force MinistryCode to current user's ministry for non-admins (prevents form tampering)
                    var (isAdminCreate, scopedMinistryCodeCreate) = await GetScopeAsync();
                    if (!isAdminCreate)
                    {
                        if (scopedMinistryCodeCreate is null)
                        {
                            ModelState.AddModelError("", _localizer["You are not assigned to a ministry."]);
                        }
                        else
                        {
                            project.MinistryCode = scopedMinistryCodeCreate;
                        }
                    }
                }

                // Repopulates the create-view ViewBag on a redisplay (validation failure, missing
                // linked indicator, etc.), keeping the program-derived ministry lock in effect.
                async Task RepopulateCreateViewAsync()
                {
                    await PopulateCreateViewBagAsync(selectedDonorCodes, selections, DonorFundingBreakdown);
                    ViewBag.PreSelectedIndicatorId = LinkedIndicatorId;
                    if (linkedProgramMinistryCode.HasValue)
                    {
                        ViewBag.IsMinistryLockedByProgram = true;
                        project.MinistryCode = linkedProgramMinistryCode;
                    }
                }

                if (!ModelState.IsValid)
                {
                    await RepopulateCreateViewAsync();
                    return View(project);
                }

                // The budget is entered in the chosen unit (e.g. "5" + Millions). Scale it up to the
                // full monetary value now, before donor funding and any other budget-based calculations.
                project.EstimatedBudget *= project.BudgetUnit.Multiplier();

                // Process donor funding
                ProcessDonorFunding(project, selectedDonorCodes, DonorFundingBreakdown);

                // Handle ministry selection
                if (project.MinistryCode.HasValue)
                {
                    var selectedMinistry = _context.Ministries.Find(project.MinistryCode.Value);
                    if (selectedMinistry != null)
                    {
                        project.Ministries = new List<Ministry> { selectedMinistry };
                    }
                }

                // Save project + link the originating indicator atomically.
                // If the connection drops mid-flow, both must roll back — otherwise we end
                // up with an orphan project that the duplicate-name check then blocks the
                // user from re-creating.
                Indicator? indicatorToLink = null;
                if (LinkedIndicatorId.HasValue)
                {
                    indicatorToLink = await _context.Indicators.FindAsync(LinkedIndicatorId.Value);
                    if (indicatorToLink == null)
                    {
                        ModelState.AddModelError("", _localizer["The indicator this project should link to could not be found. Please try again."]);
                        await RepopulateCreateViewAsync();
                        return View(project);
                    }
                }

                using (var tx = await _context.Database.BeginTransactionAsync())
                {
                    _context.Projects.Add(project);
                    await _context.SaveChangesAsync();

                    if (indicatorToLink != null)
                    {
                        indicatorToLink.ProjectID = project.ProjectID;
                        await _context.SaveChangesAsync();
                    }

                    await tx.CommitAsync();
                }

                // Create project phases: user-selected phases for "موازنة أستثمارية" donor; single auto-created phase for all others
                await CreateDefaultProjectPhasesAsync(project, hasInvestmentBudgetDonor ? selectedPhases : null, !hasInvestmentBudgetDonor);

                // Process file uploads
                await ProcessFileUploadsAsync(project.ProjectID, UploadedFiles);

                // Calculate initial DisbursementPerformance across all levels
                var monitoringService = new MonitoringService(_context);
                await monitoringService.UpdateDisbursementPerformancesForProject(project.ProjectID);

                this.SetSuccessMessage(string.Format(_localizer["Project '{0}' has been created successfully."].Value, project.ProjectName));

                // Tell the redirect target to clear the locally cached draft. Must match the
                // scoped key the Create view used (see Create.cshtml form-draft init).
                TempData["ClearDraftKey"] = LinkedIndicatorId.HasValue
                    ? $"draft:project:create:fromIndicator:{LinkedIndicatorId.Value}"
                    : "draft:project:create";

                return RedirectToAction("Details", new { id = project.ProjectID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", _localizer["An error occurred while creating the project."]);
                // Get form data for preservation
                var selectedDonorCodes = Request.Form["Donors"].ToList();
                await PopulateCreateViewBagAsync(selectedDonorCodes, selections, DonorFundingBreakdown);
                ViewBag.PreSelectedIndicatorId = LinkedIndicatorId;
                if (linkedProgramMinistryCode.HasValue)
                {
                    ViewBag.IsMinistryLockedByProgram = true;
                    project.MinistryCode = linkedProgramMinistryCode;
                }
                return View(project);
            }
        }




        [Permission(Permissions.ReadProjects)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // OPTIMIZED: Load everything in a single query with all required includes
            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .Include(p => p.Goal)
                .Include(p => p.Donors)
                .Include(p => p.ProjectDonors)
                    .ThenInclude(pd => pd.Donor)
                .Include(p => p.Ministries)
                .Include(p => p.Governorates)
                .Include(p => p.Districts)
                .Include(p => p.SubDistricts)
                .Include(p => p.Communities)
                .Include(p => p.Sector)
                .Include(p => p.PublicSectorType)
                .Include(p => p.Indicators)
                    .ThenInclude(i => i.SubOutput)
                        .ThenInclude(so => so.Output)
                            .ThenInclude(o => o.Outcome)
                                .ThenInclude(oc => oc.Framework)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.Measures)
                        .ThenInclude(m => m.Files)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                .Include(p => p.ProjectFiles)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            var (isAdminDetails, scopedMinistryCodeDetails) = await GetScopeAsync();
            if (!isAdminDetails && project.MinistryCode != scopedMinistryCodeDetails)
            {
                return Forbid();
            }

            return View(project);
        }

        // GET: Projects/PreviewDisbursementCorrection?id=5  (AJAX, SystemAdministrator only)
        // Read-only: returns the current (possibly wrong) vs corrected disbursement % without saving.
        [HttpGet]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> PreviewDisbursementCorrection(int id)
        {
            var preview = await new MonitoringService(_context).PreviewProjectDisbursementPerformance(id);
            if (preview == null) return NotFound(new { message = "Project not found." });
            return Json(preview);
        }

        // POST: Projects/CorrectDisbursementPerformance  (AJAX, SystemAdministrator only)
        // Recalculates the project's disbursement % and cascades the corrected value up the hierarchy.
        [HttpPost]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        public async Task<IActionResult> CorrectDisbursementPerformance([FromBody] CorrectDisbursementDto dto)
        {
            if (dto == null || dto.ProjectId <= 0)
                return BadRequest(new { message = "Invalid project." });

            var exists = await _context.Projects.AnyAsync(p => p.ProjectID == dto.ProjectId);
            if (!exists) return NotFound(new { message = "Project not found." });

            await new MonitoringService(_context).UpdateDisbursementPerformancesForProject(dto.ProjectId);

            var updated = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectID == dto.ProjectId);
            return Ok(new { message = "Disbursement performance corrected.", value = updated?.DisbursementPerformance });
        }


        // GET: Programs/Edit/5
        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> Edit(int? id)
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            if (id == null) return NotFound();

            // Load project + its Regions
            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .Include(p => p.Goal)
                .Include(p => p.Indicators)
                .Include(p => p.Phases)
                    .ThenInclude(pp => pp.ActionPlan)
                        .ThenInclude(ap => ap.Plans)
                .FirstOrDefaultAsync(p => p.ProjectID == id.Value);

            if (project == null) return NotFound();

            var (isAdminEditGet, scopedMinistryCodeEditGet) = await GetScopeAsync();
            if (!isAdminEditGet && project.MinistryCode != scopedMinistryCodeEditGet)
            {
                return Forbid();
            }


            // Explicitly load other collections only if needed later in the view
            await _context.Entry(project).Collection(p => p.Donors).LoadAsync();
            await _context.Entry(project).Collection(p => p.ProjectDonors).LoadAsync();
            await _context.Entry(project).Collection(p => p.Ministries).LoadAsync();
            await _context.Entry(project).Collection(p => p.Governorates).LoadAsync();
            await _context.Entry(project).Collection(p => p.Districts).LoadAsync();
            await _context.Entry(project).Collection(p => p.SubDistricts).LoadAsync();
            await _context.Entry(project).Collection(p => p.Communities).LoadAsync();
            await _context.Entry(project).Reference(p => p.Sector).LoadAsync();
            await _context.Entry(project).Collection(p => p.ProjectFiles).LoadAsync();

            if (project == null) return NotFound();

            // Build a list of selection DTOs containing names and codes
            var selectedLocations = project.Communities.Select(c => new {
                GovernorateName = isArabic ? c.SubDistrict.District.Governorate.AR_Name : c.SubDistrict.District.Governorate.EN_Name,
                GovernorateCode = c.SubDistrict.District.Governorate.Code,
                DistrictName = isArabic ? c.SubDistrict.District.AR_Name : c.SubDistrict.District.EN_Name,
                DistrictCode = c.SubDistrict.District.Code,
                SubDistrictName = isArabic ? c.SubDistrict.AR_Name : c.SubDistrict.EN_Name,
                SubDistrictCode = c.SubDistrict.Code,
                CommunityName = isArabic ? c.AR_Name : c.EN_Name,
                CommunityCode = c.Code
            }).ToList();

            ViewBag.Governorates = _context.Governorates.ToList();
            ViewBag.SelectedLocations = selectedLocations;

            // Build the Sector SelectList, marking the project's existing sector as "selected":
            var allSectors = await _context.Sectors.ToListAsync();
            ViewBag.SectorList = new SelectList(
                allSectors,
                "Code",      // value field
                isArabic ? "AR_Name" : "EN_Name",      // text field
                project.SectorCode  // selected value
            );

            ViewBag.PublicSectorTypeList = new SelectList(
                await _context.PublicSectorTypes.ToListAsync(),
                "Code",
                isArabic ? "AR_Name" : "EN_Name",
                project.PublicSectorTypeCode
            );


            // Build the Donors MultiSelectList, marking the project's existing donor codes as "selected":
            var allDonors = await _context.Donors.ToListAsync();
            // Grab an array of strings (or ints) that represent the already‐assigned donors:
            var selectedDonorCodes = project.Donors
                                        .Select(s => s.Code)      // a collection of int
                                        .ToList();

            // When you construct the MultiSelectList, pass in that "selected" list:
            ViewBag.DonorList = new MultiSelectList(
                allDonors,
                "Code",      // value field
                "Partner",      // text field
                selectedDonorCodes  // whichever codes should be pre‐checked
            );

            // Pass existing donor funding percentages to the view
            var donorFundingData = project.ProjectDonors.ToDictionary(
                pd => pd.DonorCode.ToString(),
                pd => pd.FundingPercentage
            );
            ViewBag.DonorFundingData = JsonConvert.SerializeObject(donorFundingData);

            // Build the Ministry SelectList, marking the project's existing ministry code as "selected":
            var allMinistries = await _context.Ministries.ToListAsync();
            // Get the currently selected ministry code from the first ministry in the collection
            var selectedMinistryCode = project.Ministries.FirstOrDefault()?.Code;
            // Set the MinistryCode property for binding
            project.MinistryCode = selectedMinistryCode;

            // Get the logged-in user for ministry check
            var user = await _userManager.GetUserAsync(User);
            bool isMinistryUser = false;

            // Check if the user is associated with a Ministry (and not SystemAdministrator)
            if (user?.MinistryName != null && !User.IsInRole(UserRoles.SystemAdministrator))
            {
                var userMinistry = allMinistries.FirstOrDefault(m => m.MinistryDisplayName_AR == user.MinistryName || m.MinistryDisplayName_EN == user.MinistryName || m.MinistryUserName == user.MinistryName);
                if (userMinistry != null)
                {
                    isMinistryUser = true;
                    // For ministry users, ensure the ministry code is set to their ministry
                    selectedMinistryCode = userMinistry.Code;
                    project.MinistryCode = selectedMinistryCode;
                }
            }

            ViewBag.MinistryList = new SelectList(
                allMinistries,
                "Code",      // value field
                isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN",      // text field
                selectedMinistryCode  // selected value
            );
            ViewBag.Ministries = allMinistries; // Pass full ministry list with Logo property
            ViewBag.PlatformRates = await _currencyConversion.GetFallbackRatesAsync();

            // Pass ministry user info to the view
            ViewBag.IsMinistryUser = isMinistryUser;

            // Stakeholders

            ViewBag.ProjectManager = new SelectList(await _context.ProjectManagers.ToListAsync(), "Code", "Name", project.ProjectManagerCode);
            ViewBag.SuperVisor = new SelectList(await _context.SuperVisors.ToListAsync(), "Code", "Name", project.SuperVisorCode);
            ViewBag.Goals = new SelectList(
                await _context.Goals.ToListAsync(),
                "Code",
                isArabic ? "AR_Name" : "EN_Name"
            );

            return View(project);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> UpdateProjectName(int projectId, string projectName)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && project.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            project.ProjectName = projectName;
            await _context.SaveChangesAsync();
            return Ok();
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> Edit(
          int id,
          Project project,
          List<IFormFile> UploadedFiles,
          List<int>? SelectedDonorCodes,
          string? selections,
          string? DonorFundingBreakdown)
        {
            if (id != project.ProjectID)
                return NotFound();

            // Authorization: ensure caller owns this project (or is admin)
            var existingMinistryCode = await _context.Projects
                .Where(p => p.ProjectID == id)
                .Select(p => p.MinistryCode)
                .FirstOrDefaultAsync();
            var (isAdminEditPost, scopedMinistryCodeEditPost) = await GetScopeAsync();
            if (!isAdminEditPost && existingMinistryCode != scopedMinistryCodeEditPost)
            {
                return Forbid();
            }

            // Force MinistryCode to current user's ministry for non-admins (prevents form tampering)
            if (!isAdminEditPost)
            {
                project.MinistryCode = scopedMinistryCodeEditPost;
            }

            // Initialize to empty list if null to prevent null reference exceptions and remove duplicates
            SelectedDonorCodes = SelectedDonorCodes ?? new List<int>();

            // Explicitly read IsEntireCountry from form (checkbox sends "true" if checked, nothing if unchecked)
            var isEntireCountryValue = Request.Form["IsEntireCountry"].ToString();
            bool IsEntireCountry = isEntireCountryValue.Contains("true", StringComparison.OrdinalIgnoreCase);

            // Set IsEntireCountry on project
            project.IsEntireCountry = IsEntireCountry;

            // Initialize navigation collections if necessary
            project.Governorates = new List<Governorate>();
            project.Districts = new List<District>();
            project.SubDistricts = new List<SubDistrict>();
            project.Communities = new List<Community>();

            // Process location selections only if not entire country
            if (!IsEntireCountry && !string.IsNullOrEmpty(selections))
            {
                // Deserialize JSON string into a list of location selection objects
                var selectedLocations = JsonConvert.DeserializeObject<List<LocationSelectionViewModel>>(selections);

                // Loop through each selection and add entities to the project
                foreach (var sel in selectedLocations)
                {
                    var governorate = await _context.Governorates.FindAsync(sel.GovernorateCode);
                    var district = await _context.Districts.FindAsync(sel.DistrictCode);
                    var subDistrict = await _context.SubDistricts.FindAsync(sel.SubDistrictCode);
                    var community = await _context.Communities.FindAsync(sel.CommunityCode);

                    if (governorate != null && !project.Governorates.Contains(governorate))
                        project.Governorates.Add(governorate);
                    if (district != null && !project.Districts.Contains(district))
                        project.Districts.Add(district);
                    if (subDistrict != null && !project.SubDistricts.Contains(subDistrict))
                        project.SubDistricts.Add(subDistrict);
                    if (community != null && !project.Communities.Contains(community))
                        project.Communities.Add(community);
                }
            }

            // Remove nav-props so EF Core won't demand them at bind time
            ModelState.Remove(nameof(Project.ProjectManager));
            ModelState.Remove(nameof(Project.Sector));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.Ministries));
            ModelState.Remove(nameof(Project.Ministry));
            ModelState.Remove(nameof(Project.Donors));       // <— Uncommented so EF doesn't require it
            ModelState.Remove(nameof(Project.Governorates));
            ModelState.Remove(nameof(Project.Districts));
            ModelState.Remove(nameof(Project.SubDistricts));
            ModelState.Remove(nameof(Project.Communities));
            ModelState.Remove(nameof(Project.Phases));
            ModelState.Remove(nameof(Project.Goal));
            ModelState.Remove(nameof(Project.PublicSectorType));

            // Optional on Edit: existing projects predate this field and must stay saveable.
            ValidateSectorType(project, requireSectorType: false);

            NormalizeExchangeRate(project);

            if (!ModelState.IsValid)
            {
                // If validation fails, re‐populate all dropdowns with the already‐selected codes:
                await PopulateEditDropdowns(project, SelectedDonorCodes);
                return View(project);
            }

            // --- Include Regions, Sectors, Donors, and Indicators ---
            var dbProject = await _context.Projects
                .Include(p => p.Sector)
                .Include(p => p.Donors)
                .Include(p => p.ProjectDonors)
                .Include(p => p.Ministries)
                .Include(p => p.Governorates)
                .Include(p => p.Districts)
                .Include(p => p.SubDistricts)
                .Include(p => p.Communities)
                .Include(p => p.Goal)
                .Include(p => p.Indicators)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (dbProject == null)
                return NotFound();

            // --- Update scalar properties ---
            dbProject.ProjectName = project.ProjectName;
            dbProject.StartDate = project.StartDate;
            dbProject.EndDate = project.EndDate;
            // The budget is entered in the chosen unit; scale it up to the full stored value.
            dbProject.EstimatedBudget = project.EstimatedBudget * project.BudgetUnit.Multiplier();
            dbProject.Currency = project.Currency;
            dbProject.BudgetUnit = project.BudgetUnit;
            dbProject.ExchangeRate = project.ExchangeRate;
            dbProject.ExchangeRateDate = project.ExchangeRateDate;
            dbProject.RealBudget = project.RealBudget;
            dbProject.IsEntireCountry = project.IsEntireCountry;

            dbProject.Governorates = project.Governorates;
            dbProject.Districts = project.Districts;
            dbProject.SubDistricts = project.SubDistricts;
            dbProject.Communities = project.Communities;
            dbProject.ProjectManagerCode = project.ProjectManagerCode;
            dbProject.SuperVisorCode = project.SuperVisorCode;
            dbProject.GoalCode = project.GoalCode;
            dbProject.PublicSectorTypeCode = project.PublicSectorTypeCode;
            dbProject.SectorCode = project.SectorCode;

            // --- Update Donors with funding percentages ---
            // Clear existing project donors
            dbProject.ProjectDonors.Clear();

            // Only process donors if any are selected
            if (SelectedDonorCodes.Any())
            {
                // Process donor funding breakdown if provided
                if (!string.IsNullOrEmpty(DonorFundingBreakdown))
                {
                    try
                    {
                        var fundingData = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(DonorFundingBreakdown);

                        foreach (var donorCode in SelectedDonorCodes)
                        {
                            var donor = await _context.Donors.FindAsync(donorCode);
                            if (donor != null)
                            {
                                var fundingPercentage = fundingData.ContainsKey(donorCode.ToString())
                                    ? fundingData[donorCode.ToString()]
                                    : 0;

                                var fundingAmount = (decimal)dbProject.EstimatedBudget * (fundingPercentage / 100);

                                var projectDonor = new ProjectDonor
                                {
                                    ProjectId = dbProject.ProjectID,
                                    DonorCode = donorCode,
                                    FundingPercentage = fundingPercentage,
                                    FundingAmount = fundingAmount
                                };

                                dbProject.ProjectDonors.Add(projectDonor);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // If JSON parsing fails, fall back to creating ProjectDonor records with 0% funding
                        foreach (var donorCode in SelectedDonorCodes)
                        {
                            var donor = await _context.Donors.FindAsync(donorCode);
                            if (donor != null)
                            {
                                var projectDonor = new ProjectDonor
                                {
                                    ProjectId = dbProject.ProjectID,
                                    DonorCode = donorCode,
                                    FundingPercentage = 0,
                                    FundingAmount = 0
                                };

                                dbProject.ProjectDonors.Add(projectDonor);
                            }
                        }
                    }
                }
                else
                {
                    // No funding breakdown provided, create ProjectDonor records with 0% funding
                    foreach (var donorCode in SelectedDonorCodes)
                    {
                        var donor = await _context.Donors.FindAsync(donorCode);
                        if (donor != null)
                        {
                            var projectDonor = new ProjectDonor
                            {
                                ProjectId = dbProject.ProjectID,
                                DonorCode = donorCode,
                                FundingPercentage = 0,
                                FundingAmount = 0
                            };

                            dbProject.ProjectDonors.Add(projectDonor);
                        }
                    }
                }

                // Also maintain the legacy Donors collection for backward compatibility
                var donors = await _context.Donors
                    .Where(d => SelectedDonorCodes.Contains(d.Code))
                    .ToListAsync();

                dbProject.Donors.Clear();
                foreach (var d in donors)
                    dbProject.Donors.Add(d);
            }
            else
            {
                // No donors selected, clear the legacy Donors collection
                dbProject.Donors.Clear();
            }



            // Handle single Ministry selection - keep the collection for backward compatibility
            dbProject.Ministries.Clear();
            if (project.MinistryCode.HasValue)
            {
                var selectedMinistry = await _context.Ministries.FindAsync(project.MinistryCode.Value);
                if (selectedMinistry != null)
                {
                    dbProject.Ministries.Add(selectedMinistry);
                    dbProject.MinistryCode = project.MinistryCode.Value;
                }
            }
            else
            {
                dbProject.MinistryCode = null;
            }

            // --- Handle file uploads (unchanged) ---
            if (UploadedFiles != null && UploadedFiles.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var file in UploadedFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueName);

                        using var fs = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(fs);

                        _context.ProjectFiles.Add(new ProjectFile
                        {
                            ProjectId = dbProject.ProjectID,
                            FileName = file.FileName,
                            FilePath = "/uploads/" + uniqueName
                        });
                    }
                }
            }

            // Save changes
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) when (!_context.Projects.Any(e => e.ProjectID == id))
            {
                return NotFound();
            }

            // Recalculate DisbursementPerformance across all levels since EstimatedBudget may have changed
            var monitoringService = new MonitoringService(_context);
            await monitoringService.UpdateDisbursementPerformancesForProject(id);

            // Tell the redirect target to clear the locally cached draft for this form.
            TempData["ClearDraftKey"] = $"draft:project:edit:{id}";

            return RedirectToAction("Details", new { id });
        }


        // Helper to DRY‑up re‑populating dropdowns on POST failure
        private async Task PopulateEditDropdowns(Project project, List<int> SelectedDonorCodes)
        {
            //To check
            //ViewBag.Governorates = new SelectList(
            //    await _context.Governorates.ToListAsync(),
            //    "Code", "Name", project.GovernorateCode);

            //ViewBag.Districts = new SelectList(
            //    await _context.Districts.Where(d => d.GovernorateCode == project.GovernorateCode).ToListAsync(),
            //    "Code", "Name", project.DistrictCode);

            //ViewBag.SubDistricts = new SelectList(
            //    await _context.SubDistricts.Where(s => s.DistrictCode == project.DistrictCode).ToListAsync(),
            //    "Code", "Name", project.SubDistrictCode);

            //ViewBag.Communities = new SelectList(
            //    await _context.Communities.Where(c => c.SubDistrictCode == project.SubDistrictCode).ToListAsync(),
            //    "Code", "Name", project.CommunityCode);


            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            var allSectors = await _context.Sectors.ToListAsync();
            ViewBag.SectorList = new SelectList(
                allSectors,
                "Code",
                isArabic ? "AR_Name" : "EN_Name",
                project.SectorCode
            );

            ViewBag.PublicSectorTypeList = new SelectList(
                await _context.PublicSectorTypes.ToListAsync(),
                "Code",
                isArabic ? "AR_Name" : "EN_Name",
                project.PublicSectorTypeCode
            );

            var allDonors = await _context.Donors.ToListAsync();
            ViewBag.DonorList = new MultiSelectList(
                allDonors,
                "Code",
                "Partner",
                SelectedDonorCodes
            );

            var allMinistries = await _context.Ministries.ToListAsync();

            // Get the logged-in user for ministry check
            var user = await _userManager.GetUserAsync(User);
            bool isMinistryUser = false;

            // Check if the user is associated with a Ministry (and not SystemAdministrator)
            if (user?.MinistryName != null && !User.IsInRole(UserRoles.SystemAdministrator))
            {
                var userMinistry = allMinistries.FirstOrDefault(m => m.MinistryDisplayName_AR == user.MinistryName || m.MinistryDisplayName_EN == user.MinistryName || m.MinistryUserName == user.MinistryName);
                if (userMinistry != null)
                {
                    isMinistryUser = true;
                    // For ministry users, ensure the ministry code is set to their ministry
                    project.MinistryCode = userMinistry.Code;
                }
            }

            ViewBag.MinistryList = new SelectList(
                allMinistries,
                "Code",
                isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN",
                project.MinistryCode
            );
            ViewBag.Ministries = allMinistries; // Drives the rich dropdown items (with logo) in Edit.cshtml
            ViewBag.PlatformRates = await _currencyConversion.GetFallbackRatesAsync();

            // Pass ministry user info to the view
            ViewBag.IsMinistryUser = isMinistryUser;
            ViewBag.UserMinistryCode = isMinistryUser ? project.MinistryCode : null;

            ViewBag.ProjectManager = new SelectList(await _context.ProjectManagers.ToListAsync(), "Code", "Name", project.ProjectManagerCode);
            ViewBag.SuperVisor = new SelectList(await _context.SuperVisors.ToListAsync(), "Code", "Name", project.SuperVisorCode);
        }


        // GET: Programs/Delete/5
        [Permission(Permissions.DeleteProject)]
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

            var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
            if (!isAdmin && program.MinistryCode != scopedMinistryCode)
            {
                return Forbid();
            }

            return View(program);
        }

        [HttpPost]
        [Permission(Permissions.DeleteProject)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = new MonitoringService(_context);
            try
            {
                // Get project info before deletion for performance recalculation
                var project = await _context.Projects
                    .Include(p => p.Indicators)
                    .FirstOrDefaultAsync(p => p.ProjectID == id);

                if (project != null)
                {
                    var (isAdmin, scopedMinistryCode) = await GetScopeAsync();
                    if (!isAdmin && project.MinistryCode != scopedMinistryCode)
                    {
                        return Forbid();
                    }

                    // Capture affected indicators before deletion
                    var affectedIndicators = project.Indicators.ToList();

                    // Delete the project and recalculate IndicatorsPerformance (from measures)
                    await service.DeleteProjectAndRecalculateAsync(id);

                    // Recalculate performance for all affected levels
                    if (affectedIndicators.Any())
                    {
                        var planService = new PlanService(_context);
                        foreach (var indicator in affectedIndicators)
                        {
                            await planService.RecalculatePerformanceAfterIndicatorDeletion(indicator);
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SystemAdministrator)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return Json(new { success = false, message = _localizer["No projects selected."].Value });

            var errors = new List<string>();
            var deletedCount = 0;

            var planService = new PlanService(_context);
            var monitoringService = new MonitoringService(_context);

            foreach (var id in ids)
            {
                try
                {
                    var project = await _context.Projects
                        .Include(p => p.Indicators)
                        .FirstOrDefaultAsync(p => p.ProjectID == id);

                    if (project == null) continue;

                    var affectedIndicators = project.Indicators.ToList();

                    await monitoringService.DeleteProjectAndRecalculateAsync(id);

                    foreach (var indicator in affectedIndicators)
                        await planService.RecalculatePerformanceAfterIndicatorDeletion(indicator);

                    deletedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Project {id}: {ex.Message}");
                }
            }

            if (errors.Any())
                return Json(new { success = false, message = string.Format(_localizer["Deleted {0} project(s) with errors."].Value, deletedCount) });

            return Json(new { success = true, message = string.Format(_localizer["{0} project(s) deleted successfully."].Value, deletedCount), deletedIds = ids });
        }

        private bool ProgramExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectID == id);
        }

        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> LinkProjectToIndicators(int projectId)
        {
            if (!await ProjectBelongsToScopeAsync(projectId))
            {
                return Forbid();
            }

            var model = new LinkProjectIndicatorViewModel
            {
                SelectedProjectId = projectId,
                Frameworks = _context.Frameworks
                    .Select(f => new SelectListItem { Value = f.Code.ToString(), Text = f.Name })
                    .ToList(),

                // Get already linked indicators
                LinkedIndicators = _context.Indicators
                .Where(i => i.ProjectID == projectId)
                .ToList()
            };

            return View(model);
        }
        [HttpPost]
        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> DeleteFile(int id)
        {
            var file = await _context.ProjectFiles.FindAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            if (!await ProjectBelongsToScopeAsync(file.ProjectId))
            {
                return Forbid();
            }

            // Delete the physical file
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            // Delete the database record
            _context.ProjectFiles.Remove(file);
            await _context.SaveChangesAsync();

            // Redirect back or return success
            return RedirectToAction("Details", new { id = file.ProjectId });
        }


        [Permission(Permissions.ReadProjects)]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var file = await _context.ProjectFiles.FindAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            if (!await ProjectBelongsToScopeAsync(file.ProjectId))
            {
                return Forbid();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, GetContentType(filePath), file.FileName);
        }

        [Permission(Permissions.ReadProjects)]
        public async Task<IActionResult> ViewFile(int id)
        {
            var file = await _context.ProjectFiles.FindAsync(id);
            if (file == null)
            {
                return NotFound();
            }

            if (!await ProjectBelongsToScopeAsync(file.ProjectId))
            {
                return Forbid();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var contentType = GetContentType(filePath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Use ContentDispositionHeaderValue to properly encode non-ASCII filenames
            var cd = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
            cd.SetHttpFileName(file.FileName);
            Response.Headers.ContentDisposition = cd.ToString();

            return File(fileBytes, contentType);
        }

        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }


        [HttpPost]
        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> LinkProjectToIndicators(LinkProjectIndicatorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-load dropdowns
                return View(model);
            }

            if (!await ProjectBelongsToScopeAsync(model.SelectedProjectId))
            {
                return Forbid();
            }

            // Logic to link indicators to the selected project
            foreach (var indicatorCode in model.SelectedIndicatorCodes)
            {
                var indicator = await _context.Indicators.FindAsync(indicatorCode);
                if (indicator != null && indicator.ProjectID != model.SelectedProjectId)
                {
                    indicator.ProjectID = model.SelectedProjectId;
                    _context.Indicators.Update(indicator);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public JsonResult GetOutcomes(int frameworkCode)
        {
            var outcomes = _context.Outcomes
                .Where(o => o.FrameworkCode == frameworkCode)
                .Select(o => new { o.Code, o.Name })
                .ToList();

            return Json(outcomes);
        }

        public JsonResult GetOutputs(int outcomeCode)
        {
            var outputs = _context.Outputs
                .Where(o => o.OutcomeCode == outcomeCode)
                .Select(o => new { o.Code, o.Name })
                .ToList();

            return Json(outputs);
        }

        public JsonResult GetSubOutputs(int outputCode)
        {
            var subOutputs = _context.SubOutputs
                .Where(s => s.OutputCode == outputCode)
                .Select(s => new { s.Code, s.Name })
                .ToList();

            return Json(subOutputs);
        }

        public JsonResult GetIndicators(int subOutputCode)
        {
            var indicators = _context.Indicators
                .Where(i => i.SubOutputCode == subOutputCode)
                .Select(i => new { i.IndicatorCode, i.Name })
                .ToList();

            return Json(indicators);
        }

        private async Task ProcessProjectLocationsAsync(Project project, string selections)
        {
            if (string.IsNullOrEmpty(selections))
                return;

            var selectedLocations = JsonConvert.DeserializeObject<List<LocationSelectionViewModel>>(selections);

            project.Governorates = new List<Governorate>();
            project.Districts = new List<District>();
            project.SubDistricts = new List<SubDistrict>();
            project.Communities = new List<Community>();

            foreach (var selection in selectedLocations)
            {
                var governorate = await _context.Governorates.FindAsync(selection.GovernorateCode);
                var district = await _context.Districts.FindAsync(selection.DistrictCode);
                var subDistrict = await _context.SubDistricts.FindAsync(selection.SubDistrictCode);
                var community = await _context.Communities.FindAsync(selection.CommunityCode);

                if (governorate != null && !project.Governorates.Contains(governorate))
                    project.Governorates.Add(governorate);
                if (district != null && !project.Districts.Contains(district))
                    project.Districts.Add(district);
                if (subDistrict != null && !project.SubDistricts.Contains(subDistrict))
                    project.SubDistricts.Add(subDistrict);
                if (community != null && !project.Communities.Contains(community))
                    project.Communities.Add(community);
            }
        }

        private void ProcessDonorFunding(Project project, List<string> selectedDonorCodes, string donorFundingBreakdown)
        {
            if (!selectedDonorCodes.Any())
                return;

            var selectedDonors = _context.Donors
                .Where(d => selectedDonorCodes.Contains(d.Code.ToString()))
                .ToList();
            project.Donors = selectedDonors;

            if (!string.IsNullOrEmpty(donorFundingBreakdown))
            {
                try
                {
                    var fundingData = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(donorFundingBreakdown);
                    CreateProjectDonorRecords(project, selectedDonorCodes, fundingData);
                }
                catch (JsonException)
                {
                    CreateProjectDonorRecordsWithZeroFunding(project, selectedDonorCodes);
                }
            }
            else
            {
                CreateProjectDonorRecordsWithZeroFunding(project, selectedDonorCodes);
            }
        }

        private void CreateProjectDonorRecords(Project project, List<string> donorCodes, Dictionary<string, decimal> fundingData)
        {
            foreach (var donorCodeStr in donorCodes)
            {
                if (int.TryParse(donorCodeStr, out int donorCode))
                {
                    var fundingPercentage = fundingData.ContainsKey(donorCodeStr) ? fundingData[donorCodeStr] : 0;
                    var fundingAmount = (decimal)project.EstimatedBudget * (fundingPercentage / 100);

                    project.ProjectDonors.Add(new ProjectDonor
                    {
                        DonorCode = donorCode,
                        FundingPercentage = fundingPercentage,
                        FundingAmount = fundingAmount
                    });
                }
            }
        }

        private void CreateProjectDonorRecordsWithZeroFunding(Project project, List<string> donorCodes)
        {
            foreach (var donorCodeStr in donorCodes)
            {
                if (int.TryParse(donorCodeStr, out int donorCode))
                {
                    project.ProjectDonors.Add(new ProjectDonor
                    {
                        DonorCode = donorCode,
                        FundingPercentage = 0,
                        FundingAmount = 0
                    });
                }
            }
        }

        private async Task<bool> ProcessFileUploadsAsync(int projectId, List<IFormFile> uploadedFiles)
        {
            if (uploadedFiles == null || !uploadedFiles.Any())
                return true;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            foreach (var file in uploadedFiles)
            {
                if (file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _context.ProjectFiles.Add(new ProjectFile
                    {
                        ProjectId = projectId,
                        FileName = file.FileName,
                        FilePath = "/uploads/" + uniqueFileName
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> LinkProjectIndicatorsAsync(int projectId, List<int> selectedIndicators)
        {
            if (selectedIndicators == null || !selectedIndicators.Any())
                return true;

            foreach (var indicatorCode in selectedIndicators)
            {
                var indicator = await _context.Indicators.FindAsync(indicatorCode);
                if (indicator != null && indicator.ProjectID != projectId)
                {
                    indicator.ProjectID = projectId;
                    _context.Indicators.Update(indicator);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private void RemoveNavigationPropertiesFromModelState()
        {
            var propertiesToRemove = new[]
            {
                nameof(Project.ProjectManager),
                nameof(Project.Sector),
                nameof(Project.Donors),
                nameof(Project.Ministries),
                nameof(Project.Ministry),
                nameof(Project.SuperVisor),
                nameof(Project.Phases),
                nameof(Project.Communities),
                nameof(Project.Districts),
                nameof(Project.SubDistricts),
                nameof(Project.Governorates),
                nameof(Project.Goal),
                nameof(Project.PublicSectorType)
            };

            foreach (var property in propertiesToRemove)
            {
                ModelState.Remove(property);
            }
        }

        // Sector type applies to every sector now; required on Create, optional on Edit so
        // pre-existing projects (created before this field became required) are never blocked from saving.
        private void ValidateSectorType(Project project, bool requireSectorType)
        {
            if (requireSectorType && !project.PublicSectorTypeCode.HasValue)
            {
                ModelState.AddModelError(nameof(Project.PublicSectorTypeCode),
                    _localizer["Please select a public sector type."]);
            }
        }

        private async Task PopulateCreateViewBagAsync(List<string> selectedDonorCodes = null, string locationSelections = null, string donorFundingBreakdown = null)
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            ViewBag.Governorates = _context.Governorates.ToList();

            ViewBag.SectorList = new SelectList(_context.Sectors, "Code", isArabic ? "AR_Name" : "EN_Name");
            ViewBag.PublicSectorTypeList = new SelectList(await _context.PublicSectorTypes.ToListAsync(), "Code", isArabic ? "AR_Name" : "EN_Name");

            // Get the logged-in user for ministry check
            var user = await _userManager.GetUserAsync(User);
            int? userMinistryCode = null;
            bool isMinistryUser = false;
            var ministries = _context.Ministries.ToList();

            // Check if the user is associated with a Ministry (and not SystemAdministrator)
            if (user?.MinistryName != null && !User.IsInRole(UserRoles.SystemAdministrator))
            {
                var userMinistry = ministries.FirstOrDefault(m => m.MinistryDisplayName_AR == user.MinistryName || m.MinistryDisplayName_EN == user.MinistryName || m.MinistryUserName == user.MinistryName);
                if (userMinistry != null)
                {
                    userMinistryCode = userMinistry.Code;
                    isMinistryUser = true;
                }
            }

            ViewBag.MinistryList = new SelectList(ministries, "Code", isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN", userMinistryCode);
            ViewBag.Ministries = ministries; // Drives the rich dropdown items (with logo) in Create.cshtml
            ViewBag.PlatformRates = await _currencyConversion.GetFallbackRatesAsync();
            ViewBag.IsMinistryUser = isMinistryUser;
            ViewBag.UserMinistryCode = userMinistryCode;

            ViewBag.ProjectManager = new SelectList(_context.ProjectManagers, "Code", "Name");
            ViewBag.SuperVisor = new SelectList(_context.SuperVisors, "Code", "Name");
            var allDonors = _context.Donors.AsEnumerable().ToList();
            ViewBag.Donor = new SelectList(
                allDonors
                    .OrderBy(d => d.IsInvestmentBudget ? 0 : 1)
                    .ThenBy(d => d.Partner),
                "Code", "Partner");
            ViewBag.InvestmentBudgetDonorCodes = allDonors
                .Where(d => d.IsInvestmentBudget)
                .Select(d => d.Code.ToString())
                .ToList();
            ViewBag.Goals = new SelectList(
                _context.Goals,
                "Code",
                isArabic ? "AR_Name" : "EN_Name"
            );
            // Preserve form data
            ViewBag.SelectedDonorCodes = selectedDonorCodes ?? new List<string>();
            ViewBag.LocationSelections = locationSelections ?? "";
            ViewBag.DonorFundingBreakdown = donorFundingBreakdown ?? "";
        }

        private int CalculateMonthsDifference(DateTime startDate, DateTime endDate)
        {
            // Calculate the difference in months between start and end dates
            int monthsDifference = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;

            // If the end day is before the start day, subtract one month
            if (endDate.Day < startDate.Day)
            {
                monthsDifference--;
            }

            // Return at least 1 month if the difference is 0 or negative
            return Math.Max(1, monthsDifference + 1); // +1 to include both start and end months
        }

        // ─────────────────────────────────────────────────────────────────────
        // Auto-create default implementation-tracking phases for a new project.
        // When singlePhaseMode is true (i.e. donor is "موازنة أستثمارية"), only
        // one phase spanning the full project duration is created.
        // ─────────────────────────────────────────────────────────────────────
        private async Task CreateDefaultProjectPhasesAsync(Project project, List<string>? selectedPhaseNames = null, bool singlePhaseMode = false)
        {
            if (singlePhaseMode)
            {
                // Create a single phase covering the entire project duration
                var singlePhase = new ProjectPhase
                {
                    Name = "موازنة أستثمارية",
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    Budget = 0,
                    Weight = 100,
                    ProjectID = project.ProjectID
                };
                _context.ProjectPhases.Add(singlePhase);
                await _context.SaveChangesAsync();

                int plansCount = ((singlePhase.EndDate.Year - singlePhase.StartDate.Year) * 12)
                                 + singlePhase.EndDate.Month - singlePhase.StartDate.Month;
                if (singlePhase.EndDate.Day < singlePhase.StartDate.Day) plansCount--;
                if (plansCount <= 0) plansCount = 1;

                var actionPlan = new ActionPlan { ProjectPhaseId = singlePhase.Id, PlansCount = plansCount };
                _context.ActionPlans.Add(actionPlan);
                await _context.SaveChangesAsync();

                await CreateDefaultPlansForActionPlanAsync(actionPlan.Code, singlePhase.StartDate, singlePhase.EndDate);
                return;
            }

            var allPhaseNames = ProjectPhase.DefaultCategoryNames;

            // Use only selected phases; fall back to all if none provided (e.g. legacy calls)
            var defaultPhaseNames = (selectedPhaseNames != null && selectedPhaseNames.Any())
                ? allPhaseNames.Where(n => selectedPhaseNames.Contains(n)).ToArray()
                : allPhaseNames;

            int count = defaultPhaseNames.Length;
            decimal equalWeight = Math.Round(100m / count, 2);
            decimal remainder = 100m - (equalWeight * count);

            for (int i = 0; i < count; i++)
            {
                var phase = new ProjectPhase
                {
                    Name = defaultPhaseNames[i],
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    Budget = 0,
                    Weight = equalWeight + (i == count - 1 ? remainder : 0),
                    ProjectID = project.ProjectID
                };

                _context.ProjectPhases.Add(phase);
                await _context.SaveChangesAsync();

                // Auto-create ActionPlan with PlansCount = months in phase
                int plansCount = ((phase.EndDate.Year - phase.StartDate.Year) * 12)
                                 + phase.EndDate.Month - phase.StartDate.Month;
                if (phase.EndDate.Day < phase.StartDate.Day) plansCount--;
                if (plansCount <= 0) plansCount = 1;

                var actionPlan = new ActionPlan
                {
                    ProjectPhaseId = phase.Id,
                    PlansCount = plansCount
                };
                _context.ActionPlans.Add(actionPlan);
                await _context.SaveChangesAsync();

                // Auto-create monthly Plans directly on ActionPlan
                await CreateDefaultPlansForActionPlanAsync(actionPlan.Code, phase.StartDate, phase.EndDate);
            }
        }

        private async Task CreateDefaultPlansForActionPlanAsync(int actionPlanCode, DateTime startDate, DateTime endDate)
        {
            var startMonth = new DateTime(startDate.Year, startDate.Month, 1);
            var endMonth   = new DateTime(endDate.Year,   endDate.Month,   1);

            var current = startMonth;
            int idx = 1;
            while (current <= endMonth)
            {
                _context.Plans.Add(new Plan
                {
                    Name = $"Plan {idx}",
                    Date = current,
                    Realised = 0,
                    ActionPlanCode = actionPlanCode
                });
                current = current.AddMonths(1);
                idx++;
            }

            await _context.SaveChangesAsync();
        }
    }

    // Payload for the manager "Correct Disbursement Performance" action.
    public class CorrectDisbursementDto
    {
        public int ProjectId { get; set; }
    }
}
