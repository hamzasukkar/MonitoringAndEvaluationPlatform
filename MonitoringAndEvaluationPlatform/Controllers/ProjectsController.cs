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

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IActivityService activityService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _activityService = activityService;
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

            ViewBag.Governorates = _context.Governorates.ToList();

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
                Sectors = sectors.Take(1).ToList()
            };

            var firstSectorCode = sectors.FirstOrDefault()?.Code;
            var firstMinistryCode = ministries.FirstOrDefault()?.Code;

            // Prepare dropdown and multiselect data
            ViewBag.Donor = new SelectList(donors, "Code", "Partner");
            ViewBag.SectorList = new MultiSelectList(sectors, "Code", "Name", new List<int> { firstSectorCode ?? 0 });
            ViewBag.MinistryList = new MultiSelectList(ministries, "Code", "MinistryDisplayName", new List<int> { firstMinistryCode ?? 0 });
            ViewBag.SuperVisor = new SelectList(supervisors, "Code", "Name");
            ViewBag.ProjectManager = new SelectList(projectManagers, "Code", "Name");

            return View(project);
        }


        [HttpPost]
        public async Task<IActionResult> Create(
        Project project,
        List<IFormFile> UploadedFiles,
        int PlansCount,
        string selections                // <— new parameter
    )
        {
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

            // Remove navigation property validation to avoid unnecessary errors
            ModelState.Remove(nameof(Project.ProjectManager));
            ModelState.Remove(nameof(Project.Sectors));
            ModelState.Remove(nameof(Project.Donors));
            ModelState.Remove(nameof(Project.Ministries));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.ActionPlan));
            ModelState.Remove(nameof(Project.Communities));
            ModelState.Remove(nameof(Project.Districts));
            ModelState.Remove(nameof(Project.SubDistricts));
            ModelState.Remove(nameof(Project.Governorates));

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
                ViewBag.Governorates = new SelectList(_context.Governorates, "Code", "Name");
                ViewBag.SectorList = new MultiSelectList(_context.Sectors, "Code", "Name");
                ViewBag.ProjectManager = new SelectList(_context.ProjectManagers, "Code", "FullName");
                ViewBag.SuperVisor = new SelectList(_context.SuperVisors, "Code", "FullName");
                ViewBag.Ministry = new SelectList(_context.Ministries, "Code", "Name");
                ViewBag.Donor = new SelectList(_context.Donors, "Code", "Name");

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

            // 1) Handle donor selection from form
            var selectedMinistryCodes = Request.Form["Ministries"].ToList();
            var selectedMinistries = _context.Ministries
                                         .Where(r => selectedMinistryCodes.Contains(r.Code.ToString()))
                                         .ToList();
            project.Ministries = selectedMinistries;

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
                // You can handle the “failure” case however you like. 
                // For instance, if the ActionPlan was somehow missing, you might 
                // set an error message and return to the View. In most normal flows,
                // it succeeds, so you can skip this or log something.
                ModelState.AddModelError("", "Unable to create activities for the new ActionPlan.");
                return View(project);
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

            return RedirectToAction("Index");
        }





        // GET: Programs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.ProjectManager)
                .Include(p => p.SuperVisor)
                .Include(p => p.Donors)
                .Include(p => p.Ministries)
                .Include(p => p.Sectors)
                .Include(p => p.ProjectFiles)
                .Include(p => p.Governorates)
                .Include(p => p.Districts)
                .Include(p => p.SubDistricts)
                .Include(p => p.Communities)
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }



        // GET: Programs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Load project + its Regions
            var project = await _context.Projects
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .Include(p => p.Ministries)
                .Include(p => p.Communities)
                .ThenInclude(c => c.SubDistrict)
                .ThenInclude(sd => sd.District)
                .ThenInclude(d => d.Governorate)
                .FirstOrDefaultAsync(p => p.ProjectID == id.Value);

            if (project == null) return NotFound();

            // Build a list of selection DTOs containing names and codes
            var selectedLocations = project.Communities.Select(c => new {
                GovernorateName = c.SubDistrict.District.Governorate.Name,
                GovernorateCode = c.SubDistrict.District.Governorate.Code,
                DistrictName = c.SubDistrict.District.Name,
                DistrictCode = c.SubDistrict.District.Code,
                SubDistrictName = c.SubDistrict.Name,
                SubDistrictCode = c.SubDistrict.Code,
                CommunityName = c.Name,
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
                "Name",      // text field
                selectedSectorCodes  // whichever codes should be pre‐checked
            );


            // Build the Donors MultiSelectList, marking the project’s existing donor codes as “selected”:
            var allDonors = await _context.Donors.ToListAsync();
            // Grab an array of strings (or ints) that represent the already‐assigned donors:
            var selectedDonorCodes = project.Donors
                                        .Select(s => s.Code)      // a collection of int
                                        .ToList();

            // When you construct the MultiSelectList, pass in that “selected” list:
            ViewBag.DonorList = new MultiSelectList(
                allDonors,
                "Code",      // value field
                "Partner",      // text field
                selectedDonorCodes  // whichever codes should be pre‐checked
            );

            // Build the Donors MultiSelectList, marking the project’s existing donor codes as “selected”:
            var allMinistries = await _context.Ministries.ToListAsync();
            // Grab an array of strings (or ints) that represent the already‐assigned donors:
            var selectedMinistryCodes = project.Ministries
                                        .Select(s => s.Code)      // a collection of int
                                        .ToList();

            // When you construct the MultiSelectList, pass in that “selected” list:
            ViewBag.MinistryList = new MultiSelectList(
                allMinistries,
                "Code",      // value field
                "MinistryDisplayName",      // text field
                selectedMinistryCodes  // whichever codes should be pre‐checked
            );

            // Stakeholders

            ViewBag.ProjectManager = new SelectList(await _context.ProjectManagers.ToListAsync(), "Code", "Name", project.ProjectManagerCode);
            ViewBag.SuperVisor = new SelectList(await _context.SuperVisors.ToListAsync(), "Code", "Name", project.SuperVisorCode);
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
          List<int> selectedMinistryCodes,
            string selections)
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
            ModelState.Remove(nameof(Project.Donors));       // <— Uncommented so EF doesn’t require it
            ModelState.Remove(nameof(Project.Governorates));
            ModelState.Remove(nameof(Project.Districts));
            ModelState.Remove(nameof(Project.SubDistricts));
            ModelState.Remove(nameof(Project.Communities));
            ModelState.Remove(nameof(Project.ActionPlan));

            if (!ModelState.IsValid)
            {
                // If validation fails, re‐populate all dropdowns with the already‐selected codes:
                await PopulateEditDropdowns(project, SelectedSectorCodes, SelectedDonorCodes, selectedMinistryCodes);
                return View(project);
            }

            // --- Include Regions, Sectors, and Donors so Clear() will delete old join rows ---
            var dbProject = await _context.Projects
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .Include(p => p.Ministries)
                .Include(p => p.Governorates)
                .Include(p => p.Districts)
                .Include(p => p.SubDistricts)
                .Include(p => p.Communities)
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



            // --- Update Sectors many‐to‐many ---
            var sectors = await _context.Sectors
                .Where(s => SelectedSectorCodes.Contains(s.Code))
                .ToListAsync();

            dbProject.Sectors.Clear();
            foreach (var s in sectors)
                dbProject.Sectors.Add(s);

            // --- Update Donors many‐to‐many ---
            var donors = await _context.Donors
                .Where(d => SelectedDonorCodes.Contains(d.Code))
                .ToListAsync();

            dbProject.Donors.Clear();
            foreach (var d in donors)
                dbProject.Donors.Add(d);



            // 1) Get the complete, up-to-date list of Ministry entities to keep
            var newMinistrySet = await _context.Ministries
                .Where(m => selectedMinistryCodes.Contains(m.Code))
                .ToListAsync();

            // 2) Replace the collection on the tracked entity
            dbProject.Ministries.Clear();                // remove all current links
            foreach (var min in newMinistrySet)          // add the newly selected ones
            {
                dbProject.Ministries.Add(min);
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
        private async Task PopulateEditDropdowns(Project project, List<int> SelectedSectorCodes, List<int> SelectedDonorCodes, List<int> selectedMinistryCodes)
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
            ViewBag.MinistryList = new MultiSelectList(
                allMinistries,
                "Code",
                "MinistryDisplayName",
                selectedMinistryCodes
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
