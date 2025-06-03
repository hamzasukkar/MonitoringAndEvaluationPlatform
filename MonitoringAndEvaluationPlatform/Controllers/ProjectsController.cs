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
            filter.Regions = await _context.Regions.ToListAsync();
            filter.Donors = await _context.Donors.ToListAsync();

            // Get the logged-in user
            var user = await _userManager.GetUserAsync(User);

            // Start with base project query
            var projectQuery = _context.Projects.AsQueryable();

            // If the user is associated with a Ministry, filter projects to only that Ministry
            if (user?.MinistryName != null)
            {
                projectQuery = projectQuery.Where(p => p.Ministry.MinistryName == user.MinistryName);
                filter.IsMinistryUser = true;
            }

            // Apply additional filters
            if (filter.SelectedMinistries.Any())
            {
                projectQuery = projectQuery.Where(p => filter.SelectedMinistries.Contains(p.MinistryCode));
            }

            if (filter.SelectedRegions.Any())
            {
                projectQuery = projectQuery.Where(p => p.Regions.Any(r => filter.SelectedRegions.Contains(r.Code)));
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
        // GET: Programs/Create
        public IActionResult Create()
        {
            // Retrieve related data
            var donors = _context.Donors.ToList();
            var regions = _context.Regions.ToList();
            var sectors = _context.Sectors.ToList();
            var ministries = _context.Ministries.ToList();
            var supervisors = _context.SuperVisors.ToList();
            var projectManagers = _context.ProjectManagers.ToList();
            var governorates = _context.Governorates
             .Select(g => new { g.Code, g.Name })
             .ToList();

            // Initialize project with defaults
            var project = new Project
            {
                EstimatedBudget = 0,
                RealBudget = 0,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                //DonorCode = donors.FirstOrDefault()?.Code ?? 0,//To Check
                MinistryCode = ministries.FirstOrDefault()?.Code ?? 0,
                SuperVisorCode = supervisors.FirstOrDefault()?.Code ?? 0,
                ProjectManagerCode = projectManagers.FirstOrDefault()?.Code ?? 0,
                Sectors = sectors.Take(1).ToList()
            };

            var firstSectorCode = sectors.FirstOrDefault()?.Code;

            // Prepare dropdown and multiselect data
            ViewBag.Donor = new SelectList(donors, "Code", "Partner");
            ViewBag.RegionList = new MultiSelectList(regions, "Code", "Name");
            ViewBag.SectorList = new MultiSelectList(sectors, "Code", "Name", new List<int> { firstSectorCode ?? 0 });
            ViewBag.Ministry = new SelectList(ministries, "Code", "MinistryName");
            ViewBag.SuperVisor = new SelectList(supervisors, "Code", "Name");
            ViewBag.ProjectManager = new SelectList(projectManagers, "Code", "Name");
            ViewBag.Governorates = new SelectList(governorates, "Code", "Name");

            return View(project);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
        Project project,
        List<IFormFile> UploadedFiles,
        int PlansCount                // <— new parameter
    )
        {
            // Remove navigation property validation to avoid unnecessary errors
            ModelState.Remove(nameof(Project.ProjectManager));
            ModelState.Remove(nameof(Project.Sectors));
            //To Check
            //ModelState.Remove(nameof(Project.Donor));
            ModelState.Remove(nameof(Project.Ministry));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.ActionPlan));
            ModelState.Remove(nameof(Project.Community));
            ModelState.Remove(nameof(Project.District));
            ModelState.Remove(nameof(Project.SubDistrict));
            ModelState.Remove(nameof(Project.Governorate));

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
                ViewBag.RegionList = new MultiSelectList(_context.Regions, "Code", "Name");
                ViewBag.SectorList = new MultiSelectList(_context.Sectors, "Code", "Name");
                ViewBag.ProjectManager = new SelectList(_context.ProjectManagers, "Code", "FullName");
                ViewBag.SuperVisor = new SelectList(_context.SuperVisors, "Code", "FullName");
                ViewBag.Ministry = new SelectList(_context.Ministries, "Code", "Name");
                ViewBag.Donor = new SelectList(_context.Donors, "Code", "Name");

                return View(project);
            }

            // 1) Handle region selection from form
            var selectedRegionCodes = Request.Form["Regions"].ToList();
            var selectedRegions = _context.Regions
                                         .Where(r => selectedRegionCodes.Contains(r.Code.ToString()))
                                         .ToList();
            project.Regions = selectedRegions;

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
                .Include(p => p.Regions)
                .Include(p => p.Sectors)
                .Include(p => p.ProjectFiles)
                .Include(p => p.Governorate)
                .Include(p => p.District)
                .Include(p => p.SubDistrict)
                .Include(p => p.Community)
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }



        // GET: Programs/Edit/5
        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Load project + its Regions
            var project = await _context.Projects
                .Include(p => p.Regions)
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .FirstOrDefaultAsync(p => p.ProjectID == id.Value);

            if (project == null) return NotFound();

            // --- Populate dropdown data ---

            // Governorates
            var allGovs = await _context.Governorates.ToListAsync();
            ViewBag.Governorates = new SelectList(allGovs, "Code", "Name", project.GovernorateCode);

            // Districts under the selected governorate
            var districts = await _context.Districts
                .Where(d => d.GovernorateCode == project.GovernorateCode)
                .ToListAsync();
            ViewBag.Districts = new SelectList(districts, "Code", "Name", project.DistrictCode);

            // SubDistricts under the selected district
            var subs = await _context.SubDistricts
                .Where(s => s.DistrictCode == project.DistrictCode)
                .ToListAsync();
            ViewBag.SubDistricts = new SelectList(subs, "Code", "Name", project.SubDistrictCode);

            // Communities under the selected sub-district
            var comms = await _context.Communities
                .Where(c => c.SubDistrictCode == project.SubDistrictCode)
                .ToListAsync();
            ViewBag.Communities = new SelectList(comms, "Code", "Name", project.CommunityCode);

            // Regions multi‑select (pre‑select the ones already on the project)
            var allRegions = await _context.Regions.ToListAsync();
            var selectedRegionCodes = project.Regions.Select(r => r.Code.ToString()).ToArray();
            ViewBag.RegionList = new MultiSelectList(allRegions, "Code", "Name", selectedRegionCodes);

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


            // Stakeholders

            ViewBag.ProjectManager = new SelectList(await _context.ProjectManagers.ToListAsync(), "Code", "Name", project.ProjectManagerCode);
            ViewBag.SuperVisor = new SelectList(await _context.SuperVisors.ToListAsync(), "Code", "Name", project.SuperVisorCode);
            ViewBag.Ministry = new SelectList(await _context.Ministries.ToListAsync(), "Code", "MinistryName", project.MinistryCode);

            //To Check
            //ViewBag.Donor = new SelectList(await _context.Donors.ToListAsync(), "Code", "Partner", project.DonorCode);

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





        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project, List<IFormFile> UploadedFiles, List<int> SelectedSectorCodes, List<int> SelectedDonorCodes)
        {
            if (id != project.ProjectID)
                return NotFound();

            // Remove nav‑props so EF Core won't demand them
            ModelState.Remove(nameof(Project.Regions));
            ModelState.Remove(nameof(Project.ProjectManager));
            ModelState.Remove(nameof(Project.Sectors));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.Ministry));
            //To Check
           // ModelState.Remove(nameof(Project.Donor));
            ModelState.Remove(nameof(Project.Governorate));
            ModelState.Remove(nameof(Project.District));
            ModelState.Remove(nameof(Project.SubDistrict));
            ModelState.Remove(nameof(Project.Community));
            ModelState.Remove(nameof(Project.ActionPlan));

            if (!ModelState.IsValid)
            {
                // If we fail, re‑populate all ViewBag lists exactly as in GET:
                await PopulateEditDropdowns(project, SelectedSectorCodes, SelectedDonorCodes);
                return View(project);
            }

            // Fetch the existing entity (to update its nav‑props safely)
            var dbProject = await _context.Projects
                .Include(p => p.Regions)
                .FirstOrDefaultAsync(p => p.ProjectID == id);

            if (dbProject == null) return NotFound();

            // --- Update scalar properties ---
            dbProject.ProjectName = project.ProjectName;
            dbProject.StartDate = project.StartDate;
            dbProject.EndDate = project.EndDate;
            dbProject.EstimatedBudget = project.EstimatedBudget;
            dbProject.RealBudget = project.RealBudget;

            dbProject.GovernorateCode = project.GovernorateCode;
            dbProject.DistrictCode = project.DistrictCode;
            dbProject.SubDistrictCode = project.SubDistrictCode;
            dbProject.CommunityCode = project.CommunityCode;
            dbProject.ProjectManagerCode = project.ProjectManagerCode;
            dbProject.SuperVisorCode = project.SuperVisorCode;
            dbProject.MinistryCode = project.MinistryCode;
            //To Check
            //dbProject.DonorCode = project.DonorCode;

            // --- Update Regions many‑to‑many ---
            var selectedRegionCodes = Request.Form["Regions"].ToList();
            var selectedRegions = await _context.Regions
                .Where(r => selectedRegionCodes.Contains(r.Code.ToString()))
                .ToListAsync();

            dbProject.Regions.Clear();
            foreach (var r in selectedRegions)
                dbProject.Regions.Add(r);

            // Now overwrite the many‐to‐many Sectors:
            var sectors = await _context.Sectors
                .Where(s => SelectedSectorCodes.Contains(s.Code))
                .ToListAsync();

            // Clear out existing ones, then assign the newly chosen list:
            dbProject.Sectors.Clear();
            foreach (var sec in sectors)
            {
                dbProject.Sectors.Add(sec);
            }

            // Now overwrite the many‐to‐many Sectors:
            var donors = await _context.Donors
                .Where(s => SelectedDonorCodes.Contains(s.Code))
                .ToListAsync();

            // Clear out existing ones, then assign the newly chosen list:
            dbProject.Donors.Clear();
            foreach (var don in donors)
            {
                dbProject.Donors.Add(don);
            }

            // --- Handle any new file uploads ---
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

            // Persist everything
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
            ViewBag.Governorates = new SelectList(
                await _context.Governorates.ToListAsync(),
                "Code", "Name", project.GovernorateCode);

            ViewBag.Districts = new SelectList(
                await _context.Districts.Where(d => d.GovernorateCode == project.GovernorateCode).ToListAsync(),
                "Code", "Name", project.DistrictCode);

            ViewBag.SubDistricts = new SelectList(
                await _context.SubDistricts.Where(s => s.DistrictCode == project.DistrictCode).ToListAsync(),
                "Code", "Name", project.SubDistrictCode);

            ViewBag.Communities = new SelectList(
                await _context.Communities.Where(c => c.SubDistrictCode == project.SubDistrictCode).ToListAsync(),
                "Code", "Name", project.CommunityCode);

            ViewBag.RegionList = new MultiSelectList(
                await _context.Regions.ToListAsync(),
                "Code", "Name",
                project.Regions.Select(r => r.Code.ToString()));

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


            ViewBag.ProjectManager = new SelectList(await _context.ProjectManagers.ToListAsync(), "Code", "Name", project.ProjectManagerCode);
            ViewBag.SuperVisor = new SelectList(await _context.SuperVisors.ToListAsync(), "Code", "Name", project.SuperVisorCode);
            ViewBag.Ministry = new SelectList(await _context.Ministries.ToListAsync(), "Code", "MinistryName", project.MinistryCode);
            //To Check
            //ViewBag.Donor = new SelectList(await _context.Donors.ToListAsync(), "Code", "Partner", project.DonorCode);
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
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var project = _context.Projects.Find(id);
            if (project == null) return NotFound();

            _context.Projects.Remove(project);
            _context.SaveChanges();
            return Ok();
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
