using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
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
        private readonly IActivityService _activityService;
        private readonly PlanService _planService;
        private readonly IProjectValidationService _validationService;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IActivityService activityService, PlanService planService, IProjectValidationService validationService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _activityService = activityService;
            _planService = planService;
            _validationService = validationService;
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
                    .Where(p => p.Sectors
                                 .Any(s => filter.SelectedSectors.Contains(s.Code)));
            }

            if (filter.SelectedDonors.Any())
            {
                projectQuery = projectQuery
                    .Where(p => p.Donors
                                 .Any(d => filter.SelectedDonors.Contains(d.Code)));
            }

            // Finalize and assign filtered results
            filter.Projects = await projectQuery.ToListAsync();

            return View(filter);
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

        // GET: Programs/Create
        [Permission(Permissions.AddProject)]
        public async Task<IActionResult> Create()
        {
            // Retrieve related data
            var donors = _context.Donors.ToList();
            var sectors = _context.Sectors.ToList();
            var ministries = _context.Ministries.ToList();
            var supervisors = _context.SuperVisors.ToList();
            var projectManagers = _context.ProjectManagers.ToList();
            var goals = _context.Goals.ToList();
            var indicators = _context.Indicators.OrderBy(i => i.IndicatorCode).ToList();
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            ViewBag.Governorates = _context.Governorates.ToList();
            ViewBag.Indicators = indicators;

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

            // Initialize project with defaults
            var project = new Project
            {
                EstimatedBudget = 0,
                RealBudget = 0,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                //DonorCode = donors.FirstOrDefault()?.Code ?? 0,//To Check
                MinistryCode = userMinistryCode, // Set ministry for ministry users
                SuperVisorCode = supervisors.FirstOrDefault()?.Code ?? 0,
                ProjectManagerCode = projectManagers.FirstOrDefault()?.Code ?? 0,
                Sectors = sectors.Take(1).ToList(),
                GoalCode = goals.FirstOrDefault()?.Code ?? 0,
            };

            var firstSectorCode = sectors.FirstOrDefault()?.Code;

            // Prepare dropdown and multiselect data
            ViewBag.Donor = new SelectList(donors, "Code", "Partner");
            ViewBag.SectorList = new MultiSelectList(sectors, "Code", "AR_Name", firstSectorCode.HasValue ? new List<int> { firstSectorCode.Value } : new List<int>());
            ViewBag.MinistryList = new SelectList(ministries, "Code", isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN", userMinistryCode);
            ViewBag.Ministries = ministries; // Pass full ministry list with Logo property
            ViewBag.SuperVisor = new SelectList(supervisors, "Code", "Name");

            // Pass ministry user info to the view
            ViewBag.IsMinistryUser = isMinistryUser;
            ViewBag.UserMinistryCode = userMinistryCode;

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
            int PlansCount,
            string? selections,
            List<int>? SelectedIndicators,
            string? DonorFundingBreakdown)
        {
            try
            {
                // Remove navigation properties from model state FIRST, before any validation
                RemoveNavigationPropertiesFromModelState();

                // Remove form parameters from ModelState (they're not part of the Project model)
                ModelState.Remove("selections");
                ModelState.Remove("SelectedIndicators");
                ModelState.Remove("DonorFundingBreakdown");
                ModelState.Remove("PlansCount");
                ModelState.Remove("IsEntireCountry");

                // Explicitly read IsEntireCountry from form (checkbox sends "true" if checked, nothing if unchecked)
                var isEntireCountryValue = Request.Form["IsEntireCountry"].ToString();
                bool IsEntireCountry = isEntireCountryValue.Contains("true", StringComparison.OrdinalIgnoreCase);

                // Set IsEntireCountry on project
                project.IsEntireCountry = IsEntireCountry;

                // Ensure SelectedIndicators is not null
                SelectedIndicators = SelectedIndicators ?? new List<int>();

                // Calculate PlansCount automatically based on the difference in months between StartDate and EndDate
                PlansCount = CalculateMonthsDifference(project.StartDate, project.EndDate);

                // Ensure PlansCount is at least 1 month
                if (PlansCount <= 0)
                {
                    PlansCount = 1;
                }

                // Process location selections (skip if entire country)
                if (!IsEntireCountry)
                {
                    await ProcessProjectLocationsAsync(project, selections);
                }

                // Get form data
                var selectedSectorCodes = Request.Form["Sectors"].ToList();
                var selectedDonorCodes = Request.Form["Donors"].ToList();
                var selectedLocations = string.IsNullOrEmpty(selections)
                    ? new List<LocationSelectionViewModel>()
                    : JsonConvert.DeserializeObject<List<LocationSelectionViewModel>>(selections);

                // Validate project creation
                _validationService.ValidateProjectCreation(
                    project,
                    selectedLocations,
                    selectedSectorCodes,
                    SelectedIndicators,
                    PlansCount,
                    ModelState,
                    IsEntireCountry);

                if (!ModelState.IsValid)
                {
                    await PopulateCreateViewBagAsync(selectedSectorCodes, selectedDonorCodes, SelectedIndicators, selections, DonorFundingBreakdown);
                    return View(project);
                }

                // Process sectors
                var selectedSectors = _context.Sectors
                    .Where(s => selectedSectorCodes.Contains(s.Code.ToString()))
                    .ToList();
                project.Sectors = selectedSectors;

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

                // Create action plan
                project.ActionPlan = new ActionPlan { PlansCount = PlansCount };

                // Save project and action plan
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // Create activities for the project
                var baseActivity = new Activity
                {
                    Name = project.ProjectName ?? "New Project Activity",
                    ActionPlanCode = project.ActionPlan.Code
                };

                var activitiesCreated = await _activityService.CreateActivitiesForAllTypesAsync(baseActivity);
                if (!activitiesCreated)
                {
                    ModelState.AddModelError("", "Unable to create activities for the new ActionPlan.");
                    await PopulateCreateViewBagAsync(selectedSectorCodes, selectedDonorCodes, SelectedIndicators, selections, DonorFundingBreakdown);
                    return View(project);
                }

                // Distribute budget across plans
                var actionPlanWithActivities = await _context.ActionPlans
                    .Include(ap => ap.Project)
                    .Include(ap => ap.Activities)
                        .ThenInclude(a => a.Plans)
                    .FirstOrDefaultAsync(ap => ap.Code == project.ActionPlan.Code);

                if (actionPlanWithActivities != null)
                {
                    actionPlanWithActivities.DistributeBudgetEquallyToPlans();
                    await _context.SaveChangesAsync();
                }

                // Process file uploads
                await ProcessFileUploadsAsync(project.ProjectID, UploadedFiles);

                // Link indicators to project
                await LinkProjectIndicatorsAsync(project.ProjectID, SelectedIndicators);

                // Set success message
                var indicatorCount = SelectedIndicators?.Count ?? 0;
                var successMessage = indicatorCount > 0
                    ? $"Project '{project.ProjectName}' has been created successfully with {indicatorCount} indicator(s) linked and {PlansCount} month(s) planned."
                    : $"Project '{project.ProjectName}' has been created successfully.";

                this.SetSuccessMessage(successMessage);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while creating the project: {ex.Message}");
                // Get form data for preservation
                var selectedSectorCodes = Request.Form["Sectors"].ToList();
                var selectedDonorCodes = Request.Form["Donors"].ToList();
                await PopulateCreateViewBagAsync(selectedSectorCodes, selectedDonorCodes, SelectedIndicators, selections, DonorFundingBreakdown);
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
                .Include(p => p.Sectors)
                .Include(p => p.ProjectIndicators)
                .Include(p => p.ProjectFiles) // FIXED: Missing ProjectFiles
                .AsNoTracking() // OPTIMIZATION: Read-only query for better performance
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
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
                .FirstOrDefaultAsync(p => p.ProjectID == id.Value);


            // Explicitly load other collections only if needed later in the view
            await _context.Entry(project).Collection(p => p.Donors).LoadAsync();
            await _context.Entry(project).Collection(p => p.ProjectDonors).LoadAsync();
            await _context.Entry(project).Collection(p => p.Ministries).LoadAsync();
            await _context.Entry(project).Collection(p => p.Governorates).LoadAsync();
            await _context.Entry(project).Collection(p => p.Districts).LoadAsync();
            await _context.Entry(project).Collection(p => p.SubDistricts).LoadAsync();
            await _context.Entry(project).Collection(p => p.Communities).LoadAsync();
            await _context.Entry(project).Collection(p => p.Sectors).LoadAsync();
            // ... and so on for other collections

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

            // Build the Sectors MultiSelectList, marking the project's existing sector codes as "selected":
            var allSectors = await _context.Sectors.ToListAsync();
            // Grab an array of strings (or ints) that represent the already‐assigned sectors:
            var selectedSectorCodes = project.Sectors
                                        .Select(s => s.Code)      // a collection of int
                                        .ToList();

            // When you construct the MultiSelectList, pass in that "selected" list:
            ViewBag.SectorList = new MultiSelectList(
                allSectors,
                "Code",      // value field
                 isArabic ? "AR_Name" : "EN_Name",      // text field
                selectedSectorCodes  // whichever codes should be pre‐checked
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
        public IActionResult UpdateProjectName(int projectId, string projectName)
        {
            var project = _context.Projects.Find(projectId);
            if (project == null) return NotFound();

            project.ProjectName = projectName;
            _context.SaveChanges();
            return Ok();
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permission(Permissions.EditProject)]
        public async Task<IActionResult> Edit(
          int id,
          Project project,
          List<IFormFile> UploadedFiles,
          List<int>? SelectedSectorCodes,
          List<int>? SelectedDonorCodes,
          string? selections,
          string? DonorFundingBreakdown)
        {
            if (id != project.ProjectID)
                return NotFound();

            // Initialize to empty lists if null to prevent null reference exceptions
            SelectedSectorCodes = SelectedSectorCodes ?? new List<int>();
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
            ModelState.Remove(nameof(Project.Sectors));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.Ministries));
            ModelState.Remove(nameof(Project.Ministry));
            ModelState.Remove(nameof(Project.Donors));       // <— Uncommented so EF doesn't require it
            ModelState.Remove(nameof(Project.Governorates));
            ModelState.Remove(nameof(Project.Districts));
            ModelState.Remove(nameof(Project.SubDistricts));
            ModelState.Remove(nameof(Project.Communities));
            ModelState.Remove(nameof(Project.ActionPlan));
            ModelState.Remove(nameof(Project.Goal));

            if (!ModelState.IsValid)
            {
                // If validation fails, re‐populate all dropdowns with the already‐selected codes:
                await PopulateEditDropdowns(project, SelectedSectorCodes, SelectedDonorCodes);
                return View(project);
            }

            // --- Include Regions, Sectors, and Donors so Clear() will delete old join rows ---
            var dbProject = await _context.Projects
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .Include(p => p.ProjectDonors)
                .Include(p => p.Ministries)
                .Include(p => p.Governorates)
                .Include(p => p.Districts)
                .Include(p => p.SubDistricts)
                .Include(p => p.Communities)
                .Include(p => p.Goal)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (dbProject == null)
                return NotFound();

            // --- Update scalar properties ---
            dbProject.ProjectName = project.ProjectName;
            dbProject.StartDate = project.StartDate;
            dbProject.EndDate = project.EndDate;
            dbProject.EstimatedBudget = project.EstimatedBudget;
            dbProject.RealBudget = project.RealBudget;
            dbProject.IsEntireCountry = project.IsEntireCountry;

            dbProject.Governorates = project.Governorates;
            dbProject.Districts = project.Districts;
            dbProject.SubDistricts = project.SubDistricts;
            dbProject.Communities = project.Communities;
            dbProject.ProjectManagerCode = project.ProjectManagerCode;
            dbProject.SuperVisorCode = project.SuperVisorCode;
            dbProject.GoalCode = project.GoalCode;



            // --- Update Sectors many‐to‐many ---
            var sectors = await _context.Sectors
                .Where(s => SelectedSectorCodes.Contains(s.Code))
                .ToListAsync();

            dbProject.Sectors.Clear();
            foreach (var s in sectors)
                dbProject.Sectors.Add(s);

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

            return RedirectToAction(nameof(Index));
        }


        // Helper to DRY‑up re‑populating dropdowns on POST failure
        private async Task PopulateEditDropdowns(Project project, List<int> SelectedSectorCodes, List<int> SelectedDonorCodes)
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


            var allSectors = await _context.Sectors.ToListAsync();
            ViewBag.SectorList = new MultiSelectList(
                allSectors,
                "Code",
                "Name",
                SelectedSectorCodes
            );

            var allDonors = await _context.Donors.ToListAsync();
            ViewBag.DonorList = new MultiSelectList(
                allDonors,
                "Code",
                "Partner",
                SelectedDonorCodes
            );

            var allMinistries = await _context.Ministries.ToListAsync();
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            ViewBag.MinistryList = new SelectList(
                allMinistries,
                "Code",
                isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN",
                project.MinistryCode
            );


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

            return View(program);
        }

        [HttpPost]
        [Permission(Permissions.DeleteProject)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = new MonitoringService(_context);
            try
            {
                // Get project info before deletion for DisbursementPerformance recalculation
                var project = await _context.Projects
                    .Include(p => p.ProjectIndicators)
                    .FirstOrDefaultAsync(p => p.ProjectID == id);

                if (project != null)
                {
                    // Capture affected indicator IDs before deletion
                    var affectedIndicatorIds = project.ProjectIndicators
                        .Select(pi => pi.IndicatorCode)
                        .Distinct()
                        .ToList();

                    // Delete the project and recalculate IndicatorsPerformance (from measures)
                    await service.DeleteProjectAndRecalculateAsync(id);

                    // Recalculate DisbursementPerformance for all affected levels
                    if (affectedIndicatorIds.Any())
                    {
                        var planService = new PlanService(_context);
                        await planService.RecalculateIndicatorsPerformance(affectedIndicatorIds);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProgramExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectID == id);
        }

        [Permission(Permissions.EditProject)]
        public IActionResult LinkProjectToIndicators(int projectId)
        {
            var model = new LinkProjectIndicatorViewModel
            {
                SelectedProjectId = projectId,
                Frameworks = _context.Frameworks
                    .Select(f => new SelectListItem { Value = f.Code.ToString(), Text = f.Name })
                    .ToList(),

                // Get already linked indicators
                LinkedIndicators = _context.ProjectIndicators
                .Where(pi => pi.ProjectId == projectId)
                .Include(pi => pi.Indicator)
                .Select(pi => pi.Indicator)
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

            // Logic to link indicators to the selected project
            foreach (var indicatorCode in model.SelectedIndicatorCodes)
            {
                _context.ProjectIndicators.Add(new ProjectIndicator
                {
                    ProjectId = model.SelectedProjectId,
                    IndicatorCode = indicatorCode
                });
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
                if (indicator != null)
                {
                    var existingLink = await _context.ProjectIndicators
                        .FirstOrDefaultAsync(pi => pi.ProjectId == projectId && pi.IndicatorCode == indicatorCode);

                    if (existingLink == null)
                    {
                        _context.ProjectIndicators.Add(new ProjectIndicator
                        {
                            ProjectId = projectId,
                            IndicatorCode = indicatorCode
                        });
                    }
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
                nameof(Project.Sectors),
                nameof(Project.Donors),
                nameof(Project.Ministries),
                nameof(Project.Ministry),
                nameof(Project.SuperVisor),
                nameof(Project.ActionPlan),
                nameof(Project.Communities),
                nameof(Project.Districts),
                nameof(Project.SubDistricts),
                nameof(Project.Governorates),
                nameof(Project.Goal),
                "PlansCount"
            };

            foreach (var property in propertiesToRemove)
            {
                ModelState.Remove(property);
            }
        }

        private async Task PopulateCreateViewBagAsync(List<string> selectedSectorCodes = null, List<string> selectedDonorCodes = null, List<int> selectedIndicators = null, string locationSelections = null, string donorFundingBreakdown = null)
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            ViewBag.Governorates = _context.Governorates.ToList();

            // Preserve selected sectors
            var sectorCodes = selectedSectorCodes?.Select(int.Parse).ToList() ?? new List<int>();
            ViewBag.SectorList = new MultiSelectList(_context.Sectors, "Code", "AR_Name", sectorCodes);

            // Get the logged-in user for ministry check
            var user = await _userManager.GetUserAsync(User);
            int? userMinistryCode = null;
            bool isMinistryUser = false;

            // Check if the user is associated with a Ministry (and not SystemAdministrator)
            if (user?.MinistryName != null && !User.IsInRole(UserRoles.SystemAdministrator))
            {
                var ministries = _context.Ministries.ToList();
                var userMinistry = ministries.FirstOrDefault(m => m.MinistryDisplayName_AR == user.MinistryName || m.MinistryDisplayName_EN == user.MinistryName || m.MinistryUserName == user.MinistryName);
                if (userMinistry != null)
                {
                    userMinistryCode = userMinistry.Code;
                    isMinistryUser = true;
                }
            }

            ViewBag.MinistryList = new SelectList(_context.Ministries, "Code", isArabic ? "MinistryDisplayName_AR" : "MinistryDisplayName_EN", userMinistryCode);
            ViewBag.IsMinistryUser = isMinistryUser;
            ViewBag.UserMinistryCode = userMinistryCode;

            ViewBag.ProjectManager = new SelectList(_context.ProjectManagers, "Code", "Name");
            ViewBag.SuperVisor = new SelectList(_context.SuperVisors, "Code", "Name");
            ViewBag.Donor = new SelectList(_context.Donors, "Code", "Partner");
            ViewBag.Goals = new SelectList(
                _context.Goals,
                "Code",
                isArabic ? "AR_Name" : "EN_Name"
            );
            ViewBag.Indicators = _context.Indicators.OrderBy(i => i.IndicatorCode).ToList();

            // Preserve form data
            ViewBag.SelectedSectorCodes = selectedSectorCodes ?? new List<string>();
            ViewBag.SelectedDonorCodes = selectedDonorCodes ?? new List<string>();
            ViewBag.SelectedIndicators = selectedIndicators ?? new List<int>();
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
    }
}
