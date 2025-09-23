using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using MonitoringAndEvaluationPlatform.ViewModel;
using Newtonsoft.Json;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IActivityService _activityService;
        private readonly PlanService _planService;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IActivityService activityService, PlanService planService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _activityService = activityService;
            _planService = planService;
        }


        public async Task<IActionResult> ActionPlan()
        {
            return View();
        }

        // GET: Programs
        public async Task<IActionResult> Index(ProgramFilterViewModel filter)
        {
            // Load dropdown/filter data
            filter.Ministries = await _context.Ministries.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();

            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);

            // Start with base project query
            var projectQuery = _context.Projects.AsQueryable();

            // If the user is associated with a Ministry, filter projects to only that Ministry
            if (user?.MinistryName != null)
            {
                projectQuery = projectQuery
                    .Where(p => p.Ministries
                                 .Any(m => m.MinistryDisplayName == user.MinistryName));
                filter.IsMinistryUser = true;
            }

            // Apply additional filters
            if (filter.SelectedMinistries.Any())
            {
                projectQuery = projectQuery
                    .Where(p => p.Ministries
                                 .Any(m => filter.SelectedMinistries.Contains(m.Code)));
            }


            //To Fix
            //if (filter.SelectedDonors.Any())
            //{
            //    projectQuery = projectQuery.Where(p => filter.SelectedDonors.Contains(p.DonorCode));
            //}

            // Finalize and assign filtered results
            filter.Projects = await projectQuery.ToListAsync();

            return View(filter);
        }

        // APIs for cascading
        public JsonResult GetDistricts(string governorateCode)
        {
            var districts = _context.Districts.Where(d => d.GovernorateCode == governorateCode).ToList();
            return Json(districts);
        }

        public JsonResult GetSubDistricts(string districtCode)
        {
            var subs = _context.SubDistricts.Where(s => s.DistrictCode == districtCode).ToList();
            return Json(subs);
        }

        public JsonResult GetCommunities(string subDistrictCode)
        {
            var comms = _context.Communities.Where(c => c.SubDistrictCode == subDistrictCode).ToList();
            return Json(comms);
        }

        // GET: Programs/Create
        public IActionResult Create()
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

            // Initialize project with defaults
            var project = new Project
            {
                EstimatedBudget = 0,
                RealBudget = 0,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                //DonorCode = donors.FirstOrDefault()?.Code ?? 0,//To Check
                //MinistryCode = ministries.FirstOrDefault()?.Code ?? 0,
                SuperVisorCode = supervisors.FirstOrDefault()?.Code ?? 0,
                ProjectManagerCode = projectManagers.FirstOrDefault()?.Code ?? 0,
                Sectors = sectors.Take(1).ToList(),
                GoalCode = goals.FirstOrDefault()?.Code ?? 0,
            };

            var firstSectorCode = sectors.FirstOrDefault()?.Code;
            var firstMinistryCode = ministries.FirstOrDefault()?.Code;

            // Prepare dropdown and multiselect data
            ViewBag.Donor = new SelectList(donors, "Code", "Partner");
            ViewBag.SectorList = new MultiSelectList(sectors, "Code", "AR_Name", firstSectorCode.HasValue ? new List<int> { firstSectorCode.Value } : new List<int>());
            ViewBag.MinistryList = new SelectList(ministries, "Code", "MinistryDisplayName");
            ViewBag.SuperVisor = new SelectList(supervisors, "Code", "Name");

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
        public async Task<IActionResult> Create(
        Project project,
        List<IFormFile> UploadedFiles,
        int PlansCount,
        string selections,               // Location selections
        List<int> SelectedIndicators,    // Selected indicator codes
        string DonorFundingBreakdown     // Donor funding percentages
    )
        {
            // Deserialize JSON string into a list of location selection objects
            var selectedLocations = JsonConvert.DeserializeObject<List<LocationSelectionViewModel>>(selections);
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

            // Initialize navigation collections if necessary
            project.Governorates = new List<Governorate>();
            project.Districts = new List<District>();
            project.SubDistricts = new List<SubDistrict>();
            project.Communities = new List<Community>();

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

            // Remove navigation property validation to avoid unnecessary errors
            ModelState.Remove(nameof(Project.ProjectManager));
            ModelState.Remove(nameof(Project.Sectors));
            ModelState.Remove(nameof(Project.Donors));
            ModelState.Remove(nameof(Project.Ministries));
            ModelState.Remove(nameof(Project.Ministry));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.ActionPlan));
            ModelState.Remove(nameof(Project.Communities));
            ModelState.Remove(nameof(Project.Districts));
            ModelState.Remove(nameof(Project.SubDistricts));
            ModelState.Remove(nameof(Project.Governorates));
            ModelState.Remove(nameof(Project.Goal));

            if (PlansCount < 1)
            {
                // Add a ModelState error against the field name “PlansCount”
                ModelState.AddModelError(
                    "PlansCount",
                    "Plans Count must be at least 1."
                );
            }

            if (!ModelState.IsValid)
            {
                // Re-populate ViewBag dropdowns in case of validation failure
                ViewBag.Governorates = _context.Governorates.ToList();
                ViewBag.SectorList = new MultiSelectList(_context.Sectors, "Code", "AR_Name");
                ViewBag.MinistryList = new SelectList(_context.Ministries, "Code", "MinistryDisplayName");
                ViewBag.ProjectManager = new SelectList(_context.ProjectManagers, "Code", "Name");
                ViewBag.SuperVisor = new SelectList(_context.SuperVisors, "Code", "Name");
                ViewBag.Donor = new SelectList(_context.Donors, "Code", "Partner");
                ViewBag.Goals = new SelectList(
                 _context.Goals,
                 "Code",
                 isArabic ? "AR_Name" : "EN_Name"
             );
                ViewBag.Indicators = _context.Indicators.OrderBy(i => i.IndicatorCode).ToList();

                return View(project);
            }

            // 1) Handle sector selection from form
            var selectedSectorCodes = Request.Form["Sectors"].ToList();
            var selectedSectors = _context.Sectors
                                         .Where(r => selectedSectorCodes.Contains(r.Code.ToString()))
                                         .ToList();
            project.Sectors = selectedSectors;


            // 1) Handle donor selection from form
            var selectedDonorCodes = Request.Form["Donors"].ToList();
            var selectedDonors = _context.Donors
                                         .Where(r => selectedDonorCodes.Contains(r.Code.ToString()))
                                         .ToList();
            project.Donors = selectedDonors;

            // Handle donor funding breakdown
            if (!string.IsNullOrEmpty(DonorFundingBreakdown))
            {
                try
                {
                    var fundingData = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(DonorFundingBreakdown);

                    foreach (var donorCodeStr in selectedDonorCodes)
                    {
                        if (int.TryParse(donorCodeStr, out int donorCode))
                        {
                            var fundingPercentage = fundingData.ContainsKey(donorCodeStr)
                                ? fundingData[donorCodeStr]
                                : 0;

                            var fundingAmount = (decimal)project.EstimatedBudget * (fundingPercentage / 100);

                            var projectDonor = new ProjectDonor
                            {
                                DonorCode = donorCode,
                                FundingPercentage = fundingPercentage,
                                FundingAmount = fundingAmount
                            };

                            project.ProjectDonors.Add(projectDonor);
                        }
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, create ProjectDonor records with 0% funding
                    foreach (var donorCodeStr in selectedDonorCodes)
                    {
                        if (int.TryParse(donorCodeStr, out int donorCode))
                        {
                            var projectDonor = new ProjectDonor
                            {
                                DonorCode = donorCode,
                                FundingPercentage = 0,
                                FundingAmount = 0
                            };

                            project.ProjectDonors.Add(projectDonor);
                        }
                    }
                }
            }

            // Handle single Ministry selection - keep the collection for backward compatibility
            if (project.MinistryCode.HasValue)
            {
                var selectedMinistry = _context.Ministries.Find(project.MinistryCode.Value);
                if (selectedMinistry != null)
                {
                    project.Ministries = new List<Ministry> { selectedMinistry };
                }
            }

            // 2) Create the ActionPlan and attach it to the new Project
            project.ActionPlan = new ActionPlan
            {
                PlansCount = PlansCount
                // (ProjectID will get set by EF once the Project is inserted)
            };

            // 3) Save both Project and ActionPlan together
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            var baseActivity = new Activity
            {
                Name = project.ProjectName ?? "New Project Activity",
                ActionPlanCode = project.ActionPlan.Code
                // Note: do NOT fill in ActivityType here—Service will set it for each enum
            };

            // 6) Call your service to automatically generate all Activities + Plans
            var success = await _activityService.CreateActivitiesForAllTypesAsync(baseActivity);
            if (!success)
            {
                // You can handle the "failure" case however you like. 
                // For instance, if the ActionPlan was somehow missing, you might 
                // set an error message and return to the View. In most normal flows,
                // it succeeds, so you can skip this or log something.
                ModelState.AddModelError("", "Unable to create activities for the new ActionPlan.");
                return View(project);
            }

            // 7) Distribute EstimatedBudget equally across all Plans
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

            // 4) Handle file uploads exactly as before
            if (UploadedFiles != null && UploadedFiles.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                foreach (var file in UploadedFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var projectFile = new ProjectFile
                        {
                            ProjectId = project.ProjectID,
                            FileName = file.FileName,
                            FilePath = "/uploads/" + uniqueFileName
                        };

                        _context.ProjectFiles.Add(projectFile);
                    }
                }

                await _context.SaveChangesAsync();
            }

            // 5) Link selected indicators to the project
            if (SelectedIndicators != null && SelectedIndicators.Any())
            {
                foreach (var indicatorCode in SelectedIndicators)
                {
                    // Check if the indicator exists
                    var indicator = await _context.Indicators.FindAsync(indicatorCode);
                    if (indicator != null)
                    {
                        // Check if the relationship already exists
                        var existingLink = await _context.ProjectIndicators
                            .FirstOrDefaultAsync(pi => pi.ProjectId == project.ProjectID && pi.IndicatorCode == indicatorCode);
                        
                        if (existingLink == null)
                        {
                            // Create new project-indicator relationship
                            var projectIndicator = new ProjectIndicator
                            {
                                ProjectId = project.ProjectID,
                                IndicatorCode = indicatorCode
                            };
                            
                            _context.ProjectIndicators.Add(projectIndicator);
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Display success message
                TempData["SuccessMessage"] = $"Project created successfully with {SelectedIndicators.Count} indicator(s) linked.";
            }
            else
            {
                TempData["SuccessMessage"] = "Project created successfully.";
            }

            return RedirectToAction("Index");
        }




        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                // Include only the most essential relationships initially
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .Include(p => p.Goal)
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            // Explicitly load other collections only if needed later in the view
            await _context.Entry(project).Collection(p => p.Donors).LoadAsync();
            await _context.Entry(project).Collection(p => p.Ministries).LoadAsync();
            await _context.Entry(project).Collection(p => p.Governorates).LoadAsync();
            await _context.Entry(project).Collection(p => p.Districts).LoadAsync();
            await _context.Entry(project).Collection(p => p.SubDistricts).LoadAsync();
            await _context.Entry(project).Collection(p => p.Communities).LoadAsync();
            await _context.Entry(project).Collection(p => p.Sectors).LoadAsync();
            await _context.Entry(project).Collection(p => p.ProjectIndicators).LoadAsync();
            // ... and so on for other collections

            return View(project);
        }


        // GET: Programs/Edit/5
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
                GovernorateName = c.SubDistrict.District.Governorate.EN_Name,
                GovernorateCode = c.SubDistrict.District.Governorate.Code,
                DistrictName = c.SubDistrict.District.EN_Name,
                DistrictCode = c.SubDistrict.District.Code,
                SubDistrictName = c.SubDistrict.EN_Name,
                SubDistrictCode = c.SubDistrict.Code,
                CommunityName = c.EN_Name,
                CommunityCode = c.Code
            }).ToList();

            ViewBag.Governorates = _context.Governorates.ToList();
            ViewBag.SelectedLocations = selectedLocations;

            // Build the Sectors MultiSelectList, marking the project’s existing sector codes as “selected”:
            var allSectors = await _context.Sectors.ToListAsync();
            // Grab an array of strings (or ints) that represent the already‐assigned sectors:
            var selectedSectorCodes = project.Sectors
                                        .Select(s => s.Code)      // a collection of int
                                        .ToList();

            // When you construct the MultiSelectList, pass in that “selected” list:
            ViewBag.SectorList = new MultiSelectList(
                allSectors,
                "Code",      // value field
                 isArabic ? "AR_Name" : "EN_Name",      // text field
                selectedSectorCodes  // whichever codes should be pre‐checked
            );


            // Build the Donors MultiSelectList, marking the project’s existing donor codes as “selected”:
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

            ViewBag.MinistryList = new SelectList(
                allMinistries,
                "Code",      // value field
                "MinistryDisplayName",      // text field
                selectedMinistryCode  // selected value
            );

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
        public async Task<IActionResult> Edit(
          int id,
          Project project,
          List<IFormFile> UploadedFiles,
          List<int> SelectedSectorCodes,
          List<int> SelectedDonorCodes,
          string selections,
          string DonorFundingBreakdown)
        {
            if (id != project.ProjectID)
                return NotFound();

            // Deserialize JSON string into a list of location selection objects
            var selectedLocations = JsonConvert.DeserializeObject<List<LocationSelectionViewModel>>(selections);

            // Initialize navigation collections if necessary
            project.Governorates = new List<Governorate>();
            project.Districts = new List<District>();
            project.SubDistricts = new List<SubDistrict>();
            project.Communities = new List<Community>();

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
            ViewBag.MinistryList = new SelectList(
                allMinistries,
                "Code",
                "MinistryDisplayName",
                project.MinistryCode
            );


            ViewBag.ProjectManager = new SelectList(await _context.ProjectManagers.ToListAsync(), "Code", "Name", project.ProjectManagerCode);
            ViewBag.SuperVisor = new SelectList(await _context.SuperVisors.ToListAsync(), "Code", "Name", project.SuperVisorCode);
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

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = new MonitoringService(_context);
            try
            {
                // Call the recalculation method BEFORE deleting the project
                var project = await _context.Projects.FindAsync(id);

                if (project != null)
                {
                    await _planService.RecalculatePerformanceAfterProjectDeletion(project);
                }
                await service.DeleteProjectAndRecalculateAsync(id);

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
    }
}
