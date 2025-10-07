using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize(Roles = "SystemAdministrator")]
    public class ProjectManagersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectManagersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProjectManagers
        public async Task<IActionResult> Index()
        {
            return View(await _context.ProjectManagers.ToListAsync());
        }

        // GET: ProjectManagers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projectManager = await _context.ProjectManagers
                .FirstOrDefaultAsync(m => m.Code == id);
            if (projectManager == null)
            {
                return NotFound();
            }

            // Get associated projects
            var projects = await _context.Projects
                .Include(p => p.Sectors)
                .Include(p => p.Donors)
                .Include(p => p.SuperVisor)
                .Include(p => p.Ministries)
                .Include(p => p.Governorates)
                .Where(p => p.ProjectManagerCode == id)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalProjects = projects.Count;
            ViewBag.ActiveProjects = projects.Count(p => p.EndDate >= DateTime.Now);
            ViewBag.CompletedProjects = projects.Count(p => p.EndDate < DateTime.Now);
            ViewBag.TotalBudget = projects.Sum(p => p.EstimatedBudget);
            ViewBag.Projects = projects;

            return View(projectManager);
        }

        private bool ProjectManagerExists(int id)
        {
            return _context.ProjectManagers.Any(e => e.Code == id);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string Name, string PhoneNumber, string Email)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return Json(new { success = false, message = "Name is required." });
            }

            var projectManager = new ProjectManager
            {
                Name = Name,
                PhoneNumber = PhoneNumber,
                Email = Email
            };

            try
            {
                _context.ProjectManagers.Add(projectManager);
                await _context.SaveChangesAsync();
                return Json(new { success = true, projectManager = new { projectManager.Code, projectManager.Name, projectManager.PhoneNumber, projectManager.Email } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating project manager: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var projectManager = await _context.ProjectManagers.FindAsync(id);
            if (projectManager == null)
                return Json(new { success = false, message = "Project Manager not found" });

            switch (field.ToLower())
            {
                case "name":
                    projectManager.Name = value;
                    break;
                case "phonenumber":
                    projectManager.PhoneNumber = value;
                    break;
                case "email":
                    projectManager.Email = value;
                    break;
                default:
                    return Json(new { success = false, message = "Invalid field" });
            }

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var projectManager = await _context.ProjectManagers.FindAsync(id);
            if (projectManager == null)
                return Json(new { success = false, message = "Project Manager not found" });

            try
            {
                _context.ProjectManagers.Remove(projectManager);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickUpdate(int id, string name)
        {
            var projectManager = await _context.ProjectManagers.FindAsync(id);
            if (projectManager == null)
                return Json(new { success = false, message = "Project Manager not found" });

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required" });

            projectManager.Name = name;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
