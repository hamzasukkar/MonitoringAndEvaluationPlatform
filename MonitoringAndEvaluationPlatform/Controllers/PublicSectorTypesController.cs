using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    [Authorize]
    public class PublicSectorTypesController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly ILogger<PublicSectorTypesController> _logger;


        public PublicSectorTypesController(ApplicationDbContext context, ILogger<PublicSectorTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: PublicSectorTypes
        public async Task<IActionResult> Index()
        {
            var publicSectorTypes = await _context.PublicSectorTypes
                .OrderBy(t => t.Code)
                .ToListAsync();

            return View(publicSectorTypes);
        }

        // Inline Operations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string EN_Name, string AR_Name)
        {
            if (string.IsNullOrWhiteSpace(EN_Name) || string.IsNullOrWhiteSpace(AR_Name))
            {
                return Json(new { success = false, message = "English and Arabic names are required." });
            }

            var publicSectorType = new PublicSectorType
            {
                EN_Name = EN_Name,
                AR_Name = AR_Name
            };

            try
            {
                _context.PublicSectorTypes.Add(publicSectorType);
                await _context.SaveChangesAsync();
                return Json(new { success = true, publicSectorType = new { publicSectorType.Code, publicSectorType.EN_Name, publicSectorType.AR_Name } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred. Please try again or contact an administrator." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var publicSectorType = await _context.PublicSectorTypes.FindAsync(id);
            if (publicSectorType == null)
                return Json(new { success = false, message = "Public sector type not found" });

            switch (field.ToLower())
            {
                case "en_name":
                    publicSectorType.EN_Name = value;
                    break;
                case "ar_name":
                    publicSectorType.AR_Name = value;
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
                return Json(new { success = false, message = "An unexpected error occurred. Please try again or contact an administrator." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var publicSectorType = await _context.PublicSectorTypes.FindAsync(id);
            if (publicSectorType == null)
                return Json(new { success = false, message = "Public sector type not found" });

            // The FK is Restrict — surface a friendly message instead of a constraint exception
            var projectsUsingType = await _context.Projects.CountAsync(p => p.PublicSectorTypeCode == id);
            if (projectsUsingType > 0)
            {
                return Json(new { success = false, message = $"This public sector type is in use by {projectsUsingType} project(s) and cannot be deleted." });
            }

            try
            {
                _context.PublicSectorTypes.Remove(publicSectorType);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred. Please try again or contact an administrator." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickUpdate(int id, string enName, string arName)
        {
            var publicSectorType = await _context.PublicSectorTypes.FindAsync(id);
            if (publicSectorType == null)
                return Json(new { success = false, message = "Public sector type not found" });

            if (string.IsNullOrWhiteSpace(enName) || string.IsNullOrWhiteSpace(arName))
                return Json(new { success = false, message = "Both names are required" });

            publicSectorType.EN_Name = enName;
            publicSectorType.AR_Name = arName;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred. Please try again or contact an administrator." });
            }
        }
    }
}
