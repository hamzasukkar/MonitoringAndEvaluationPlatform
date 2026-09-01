using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// The Units setup section: the single place measurement units are defined, so the forms
    /// that used to take free text can offer a dropdown instead.
    ///
    /// Shaped like PublicSectorTypesController — one Index view driving JSON endpoints for
    /// inline add/edit/delete — plus <see cref="CreateAjax"/>, which is what the "add new unit"
    /// control on the other forms posts to.
    /// </summary>
    [Authorize]
    public class UnitsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UnitsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Units
        public async Task<IActionResult> Index()
        {
            var units = await _context.MeasurementUnits
                .OrderBy(u => u.EN_Name)
                .ToListAsync();

            ViewBag.UsageCounts = await UsageCountsAsync();

            return View(units);
        }

        // POST: Units/CreateInline
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInline(string EN_Name, string AR_Name, string? FR_Name)
        {
            if (string.IsNullOrWhiteSpace(EN_Name) || string.IsNullOrWhiteSpace(AR_Name))
            {
                return Json(new { success = false, message = "English and Arabic names are required." });
            }

            var (unit, error) = await CreateUnitAsync(EN_Name, AR_Name, FR_Name);
            if (unit == null)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new
            {
                success = true,
                unit = new { unit.Code, unit.EN_Name, unit.AR_Name, unit.FR_Name, unit.DisplayName }
            });
        }

        /// <summary>
        /// Creates a unit from a form elsewhere in the app — the inline "add new" on the impact
        /// indicator, framework goal and measure forms — and returns it so the caller can add it
        /// to its select and select it without a round trip.
        ///
        /// Only one name is asked for, since interrupting data entry to demand three
        /// translations would defeat the point. The other columns get the same text so the row
        /// is never half-empty, and an admin can correct it later in /Units.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Unit name is required." });
            }

            var trimmed = name.Trim();

            // An existing unit wins over a duplicate: someone typing a name that is already
            // defined should get that unit selected, not an error.
            var existing = await _context.MeasurementUnits
                .FirstOrDefaultAsync(u => u.EN_Name == trimmed || u.AR_Name == trimmed || u.FR_Name == trimmed);

            if (existing != null)
            {
                return Json(new
                {
                    success = true,
                    existed = true,
                    unit = new { existing.Code, existing.EN_Name, existing.AR_Name, existing.DisplayName }
                });
            }

            var (unit, error) = await CreateUnitAsync(EN_Name: trimmed, AR_Name: trimmed, FR_Name: null);

            if (unit == null)
            {
                return Json(new { success = false, message = error });
            }

            return Json(new
            {
                success = true,
                existed = false,
                unit = new { unit.Code, unit.EN_Name, unit.AR_Name, unit.DisplayName }
            });
        }

        // POST: Units/InlineEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InlineEdit(int id, string field, string value)
        {
            var unit = await _context.MeasurementUnits.FindAsync(id);
            if (unit == null)
                return Json(new { success = false, message = "Unit not found" });

            switch (field?.ToLower())
            {
                case "en_name":
                    if (string.IsNullOrWhiteSpace(value))
                        return Json(new { success = false, message = "English name is required." });
                    if (await NameTakenAsync(value.Trim(), id))
                        return Json(new { success = false, message = "A unit with that name already exists." });
                    unit.EN_Name = value.Trim();
                    break;
                case "ar_name":
                    if (string.IsNullOrWhiteSpace(value))
                        return Json(new { success = false, message = "Arabic name is required." });
                    if (await NameTakenAsync(value.Trim(), id))
                        return Json(new { success = false, message = "A unit with that name already exists." });
                    unit.AR_Name = value.Trim();
                    break;
                case "fr_name":
                    unit.FR_Name = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

        // POST: Units/InlineDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InlineDelete(int id)
        {
            var unit = await _context.MeasurementUnits.FindAsync(id);
            if (unit == null)
                return Json(new { success = false, message = "Unit not found" });

            // The FKs are Restrict — refuse with a message naming where it is used, rather than
            // letting a constraint violation surface as a 500.
            var usage = await UsageForAsync(id);
            if (usage.Total > 0)
            {
                return Json(new { success = false, message = usage.Describe() });
            }

            try
            {
                _context.MeasurementUnits.Remove(unit);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Units/QuickUpdate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdate(int id, string enName, string arName, string? frName)
        {
            var unit = await _context.MeasurementUnits.FindAsync(id);
            if (unit == null)
                return Json(new { success = false, message = "Unit not found" });

            if (string.IsNullOrWhiteSpace(enName) || string.IsNullOrWhiteSpace(arName))
                return Json(new { success = false, message = "English and Arabic names are required." });

            if (await NameTakenAsync(enName.Trim(), id) || await NameTakenAsync(arName.Trim(), id))
                return Json(new { success = false, message = "A unit with that name already exists." });

            unit.EN_Name = enName.Trim();
            unit.AR_Name = arName.Trim();
            unit.FR_Name = string.IsNullOrWhiteSpace(frName) ? null : frName.Trim();

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

        // ─────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────

        private async Task<(MeasurementUnit? unit, string? error)> CreateUnitAsync(
            string EN_Name, string AR_Name, string? FR_Name)
        {
            var en = EN_Name.Trim();
            var ar = AR_Name.Trim();

            if (await NameTakenAsync(en, null) || await NameTakenAsync(ar, null))
            {
                return (null, "A unit with that name already exists.");
            }

            var unit = new MeasurementUnit
            {
                EN_Name = en,
                AR_Name = ar,
                FR_Name = string.IsNullOrWhiteSpace(FR_Name) ? null : FR_Name.Trim()
            };

            try
            {
                _context.MeasurementUnits.Add(unit);
                await _context.SaveChangesAsync();
                return (unit, null);
            }
            catch (Exception ex)
            {
                return (null, "Error creating unit: " + ex.Message);
            }
        }

        private Task<bool> NameTakenAsync(string name, int? exceptCode) =>
            _context.MeasurementUnits.AnyAsync(u =>
                u.Code != (exceptCode ?? 0) &&
                (u.EN_Name == name || u.AR_Name == name));

        /// <summary>How many records of each kind reference each unit, for the whole table at once.</summary>
        private async Task<Dictionary<int, UnitUsage>> UsageCountsAsync()
        {
            var indicators = await _context.ImpactIndicators
                .Where(i => i.UnitCode != null)
                .GroupBy(i => i.UnitCode!.Value)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync();

            var goals = await _context.FrameworkGoals
                .Where(g => g.UnitCode != null)
                .GroupBy(g => g.UnitCode!.Value)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync();

            var measures = await _context.Measures
                .Where(m => m.UnitCode != null)
                .GroupBy(m => m.UnitCode!.Value)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = new Dictionary<int, UnitUsage>();

            foreach (var row in indicators)
                Slot(result, row.Code).ImpactIndicators = row.Count;
            foreach (var row in goals)
                Slot(result, row.Code).FrameworkGoals = row.Count;
            foreach (var row in measures)
                Slot(result, row.Code).Measures = row.Count;

            return result;

            static UnitUsage Slot(Dictionary<int, UnitUsage> map, int code)
            {
                if (!map.TryGetValue(code, out var usage))
                {
                    usage = new UnitUsage();
                    map[code] = usage;
                }
                return usage;
            }
        }

        private async Task<UnitUsage> UsageForAsync(int code) => new UnitUsage
        {
            ImpactIndicators = await _context.ImpactIndicators.CountAsync(i => i.UnitCode == code),
            FrameworkGoals = await _context.FrameworkGoals.CountAsync(g => g.UnitCode == code),
            Measures = await _context.Measures.CountAsync(m => m.UnitCode == code)
        };

        /// <summary>Reference counts for one unit. Also rendered on the Index table.</summary>
        public class UnitUsage
        {
            public int ImpactIndicators { get; set; }
            public int FrameworkGoals { get; set; }
            public int Measures { get; set; }

            public int Total => ImpactIndicators + FrameworkGoals + Measures;

            public string Describe()
            {
                var parts = new List<string>();
                if (ImpactIndicators > 0) parts.Add($"{ImpactIndicators} impact indicator(s)");
                if (FrameworkGoals > 0) parts.Add($"{FrameworkGoals} framework goal(s)");
                if (Measures > 0) parts.Add($"{Measures} measure(s)");
                return $"This unit is in use by {string.Join(", ", parts)} and cannot be deleted.";
            }
        }
    }
}
