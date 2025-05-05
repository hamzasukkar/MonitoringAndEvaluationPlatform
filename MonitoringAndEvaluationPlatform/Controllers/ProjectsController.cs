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
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ProjectsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
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
                projectQuery = projectQuery.Where(p => filter.SelectedRegions.Contains(p.RegionCode));
            }

            if (filter.SelectedDonors.Any())
            {
                projectQuery = projectQuery.Where(p => filter.SelectedDonors.Contains(p.DonorCode));
            }

            // Finalize and assign filtered results
            filter.Projects = await projectQuery.ToListAsync();

            return View(filter);
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
                .Include(p => p.Donor)
                .Include(p => p.Region)
                .Include(p => p.Sector)
                .Include(p => p.ProjectFiles)
                .FirstOrDefaultAsync(m => m.ProjectID == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Programs/Create
        public IActionResult Create()
        {

            ViewData["Donor"] = new SelectList(_context.Donors, "Code", "Partner");
            ViewData["Region"] = new SelectList(_context.Regions, "Code", "Name");
            ViewData["Sector"] = new SelectList(_context.Sectors, "Code", "Name");
            ViewData["Ministry"] = new SelectList(_context.Ministries, "Code", "MinistryName");
            ViewData["SuperVisor"] = new SelectList(_context.SuperVisors, "Code", "Name");
            ViewData["ProjectManager"] = new SelectList(_context.ProjectManagers, "Code", "Name");
            ViewBag.Indicators = _context.Indicators.ToList();

            return View();
        }

        // POST: Programs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {

            ModelState.Remove(nameof(Project.ProjectManager));
            ModelState.Remove(nameof(Project.Region));
            ModelState.Remove(nameof(Project.Sector));
            ModelState.Remove(nameof(Project.Donor));
            ModelState.Remove(nameof(Project.Ministry));
            ModelState.Remove(nameof(Project.SuperVisor));
            ModelState.Remove(nameof(Project.ActionPlan));

            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync(); // Save first to get Project ID

                // Handle file uploads
                if (project.UploadedFiles != null && project.UploadedFiles.Count > 0)
                {
                    foreach (var file in project.UploadedFiles)
                    {
                        if (file.Length > 0)
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            // Optional: create unique filename to avoid collision
                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Save file path to database
                            var projectFile = new ProjectFile
                            {
                                ProjectId = project.ProjectID, // Assuming your Project PK is ProjectID
                                FileName = file.FileName,       // original filename
                                FilePath = "/uploads/" + uniqueFileName // web-accessible path
                            };
                            _context.ProjectFiles.Add(projectFile);
                        }
                    }

                    await _context.SaveChangesAsync(); // Save ProjectFiles
                }

                return RedirectToAction(nameof(Index));
            }

            return View();

        }

        // GET: Programs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var program = await _context.Projects.FindAsync(id);
            if (program == null)
            {
                return NotFound();
            }
            ViewBag.Indicators = _context.Indicators.ToList();
            return View(program);
        }

        // POST: Programs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,ProjectName,EstimatedBudget,RealBudget,Trend,ProjectManager,SuperVisor,Type,Status1,Status2,Category,Donor,StartDate,EndDate,Region,performance,DisbursementPerformance,FieldMonitoring,ImpactAssessment")] Models.Project program)
        {
            if (id != program.ProjectID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(program);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProgramExists(program.ProjectID))
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
            return View(program);
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

        // POST: Programs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var program = await _context.Projects.FindAsync(id);
            if (program != null)
            {
                _context.Projects.Remove(program);
            }

            await _context.SaveChangesAsync();
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
