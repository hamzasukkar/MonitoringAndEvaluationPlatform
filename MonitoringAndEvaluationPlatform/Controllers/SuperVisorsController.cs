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
    public class SuperVisorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperVisorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: SuperVisors
        public async Task<IActionResult> Index()
        {
            return View(await _context.SuperVisors.ToListAsync());
        }

        // GET: SuperVisors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var superVisor = await _context.SuperVisors
                .FirstOrDefaultAsync(m => m.Code == id);
            if (superVisor == null)
            {
                return NotFound();
            }

            return View(superVisor);
        }

        private bool SuperVisorExists(int id)
        {
            return _context.SuperVisors.Any(e => e.Code == id);
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

            var supervisor = new SuperVisor
            {
                Name = Name,
                PhoneNumber = PhoneNumber,
                Email = Email
            };

            try
            {
                _context.SuperVisors.Add(supervisor);
                await _context.SaveChangesAsync();
                return Json(new { success = true, supervisor = new { supervisor.Code, supervisor.Name, supervisor.PhoneNumber, supervisor.Email } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating supervisor: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var supervisor = await _context.SuperVisors.FindAsync(id);
            if (supervisor == null)
                return Json(new { success = false, message = "SuperVisor not found" });

            switch (field.ToLower())
            {
                case "name":
                    supervisor.Name = value;
                    break;
                case "phonenumber":
                    supervisor.PhoneNumber = value;
                    break;
                case "email":
                    supervisor.Email = value;
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
            var supervisor = await _context.SuperVisors.FindAsync(id);
            if (supervisor == null)
                return Json(new { success = false, message = "SuperVisor not found" });

            try
            {
                _context.SuperVisors.Remove(supervisor);
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
            var supervisor = await _context.SuperVisors.FindAsync(id);
            if (supervisor == null)
                return Json(new { success = false, message = "SuperVisor not found" });

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Name is required" });

            supervisor.Name = name;

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
