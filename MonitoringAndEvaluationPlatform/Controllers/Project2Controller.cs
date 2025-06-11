using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class Project2Controller : Controller
    {
        private readonly ApplicationDbContext _context;

        public Project2Controller(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Project2
        public async Task<IActionResult> Index()
        {
            return View(await _context.project2s.ToListAsync());
        }

        // GET: Project2/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project2 = await _context.project2s
                .FirstOrDefaultAsync(m => m.ProjectID == id);
            if (project2 == null)
            {
                return NotFound();
            }

            return View(project2);
        }


        public IActionResult Create()
        {
            ViewBag.Governorates = _context.Governorates.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Project2 project, List<LocationSelectionViewModel> selections)
        {
            if (ModelState.IsValid)
            {
                foreach (var sel in selections)
                {
                    var governorate = _context.Governorates.Find(sel.GovernorateCode);
                    var district = _context.Districts.Find(sel.DistrictCode);
                    var subDistrict = _context.SubDistricts.Find(sel.SubDistrictCode);
                    var community = _context.Communities.Find(sel.CommunityCode);

                    if (governorate != null) project.Governorates.Add(governorate);
                    if (district != null) project.Districts.Add(district);
                    if (subDistrict != null) project.SubDistricts.Add(subDistrict);
                    if (community != null) project.Communities.Add(community);
                }

                _context.project2s.Add(project);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Governorates = _context.Governorates.ToList();
            return View(project);
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

        // GET: Project2/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project2 = await _context.project2s.FindAsync(id);
            if (project2 == null)
            {
                return NotFound();
            }
            return View(project2);
        }

        // POST: Project2/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,ProjectName")] Project2 project2)
        {
            if (id != project2.ProjectID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project2);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Project2Exists(project2.ProjectID))
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
            return View(project2);
        }

        // GET: Project2/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project2 = await _context.project2s
                .FirstOrDefaultAsync(m => m.ProjectID == id);
            if (project2 == null)
            {
                return NotFound();
            }

            return View(project2);
        }

        // POST: Project2/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project2 = await _context.project2s.FindAsync(id);
            if (project2 != null)
            {
                _context.project2s.Remove(project2);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool Project2Exists(int id)
        {
            return _context.project2s.Any(e => e.ProjectID == id);
        }
    }
}
