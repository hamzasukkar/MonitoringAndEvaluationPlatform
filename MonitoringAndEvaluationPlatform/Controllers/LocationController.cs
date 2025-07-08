using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.ViewModel;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    public class LocationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LocationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var locations = _context.Communities
                .Include(c => c.SubDistrict)
                    .ThenInclude(sd => sd.District)
                        .ThenInclude(d => d.Governorate)
                .Select(c => new LocationViewModel
                {
                    Community = c.Name,
                    SubDistrict = c.SubDistrict.Name,
                    District = c.SubDistrict.District.Name,
                    Governorate = c.SubDistrict.District.Governorate.Name
                }).ToList();

            return View(locations);
        }
       //To check
        //public IActionResult ProjectsWithLocations()
        //{
        //    var projects = _context.Projects
        //        .Include(p => p.Community)
        //            .ThenInclude(c => c.SubDistrict)
        //                .ThenInclude(sd => sd.District)
        //                    .ThenInclude(d => d.Governorate)
        //        .Select(p => new ProjectLocationViewModel
        //        {
        //            ProjectName = p.ProjectName,
        //            Community = p.Community.Name,
        //            SubDistrict = p.Community.SubDistrict.Name,
        //            District = p.Community.SubDistrict.District.Name,
        //            Governorate = p.Community.SubDistrict.District.Governorate.Name
        //        }).ToList();

        //    return View(projects);
        //}


        [HttpGet]
        public IActionResult CreateLocation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateLocation(LocationInputViewModel model)
        {
            if (ModelState.IsValid)
            {
                var governorate = new Governorate { Name = model.GovernorateName };
                var district = new District { Name = model.DistrictName, Governorate = governorate };
                var subDistrict = new SubDistrict { Name = model.SubDistrictName, District = district };
                var community = new Community { Name = model.CommunityName, SubDistrict = subDistrict };

                _context.Governorates.Add(governorate);
                _context.Districts.Add(district);
                _context.SubDistricts.Add(subDistrict);
                _context.Communities.Add(community);

                await _context.SaveChangesAsync();
                return RedirectToAction("Index"); // or any page you prefer
            }

            return View(model);
        }

        // List Governorates
        public async Task<IActionResult> Governorates()
        {
            var data = await _context.Governorates.ToListAsync();
            return View(data);
        }

        // Create
        [HttpGet]
        public IActionResult CreateGovernorate() => View();

        [HttpPost]
        public async Task<IActionResult> CreateGovernorate(Governorate model)
        {
            if (ModelState.IsValid)
            {
                _context.Governorates.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Governorates");
            }
            return View(model);
        }

        // List Districts
        public async Task<IActionResult> Districts()
        {
            var data = await _context.Districts.Include(d => d.Governorate).ToListAsync();
            return View(data);
        }

        // Create
        [HttpGet]
        public IActionResult CreateDistrict()
        {
            ViewBag.Governorates = new SelectList(_context.Governorates, "Code", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateDistrict(District model)
        {
            ModelState.Remove(nameof(model.Governorate));
            if (ModelState.IsValid)
            {
                _context.Districts.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Districts");
            }

            ViewBag.Governorates = new SelectList(_context.Governorates, "Code", "Name", model.GovernorateCode);
            return View(model);
        }

        // List
        public async Task<IActionResult> SubDistricts()
        {
            var data = await _context.SubDistricts.Include(s => s.District).ThenInclude(d => d.Governorate).ToListAsync();
            return View(data);
        }

        // Create
        [HttpGet]
        public IActionResult CreateSubDistrict()
        {
            ViewBag.Districts = new SelectList(_context.Districts.Include(d => d.Governorate), "Code", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubDistrict(SubDistrict model)
        {
            ModelState.Remove(nameof(model.District));

            if (ModelState.IsValid)
            {
                _context.SubDistricts.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("SubDistricts");
            }

            ViewBag.Districts = new SelectList(_context.Districts, "Code", "Name", model.DistrictCode);
            return View(model);
        }

        // List
        public async Task<IActionResult> Communities()
        {
            var data = await _context.Communities.Include(c => c.SubDistrict).ThenInclude(s => s.District).ToListAsync();
            return View(data);
        }

        // Create
        [HttpGet]
        public IActionResult CreateCommunity()
        {
            ViewBag.SubDistricts = new SelectList(_context.SubDistricts, "Code", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCommunity(Community model)
        {
            ModelState.Remove(nameof(model.SubDistrict));
            if (ModelState.IsValid)
            {
                _context.Communities.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Communities");
            }

            ViewBag.SubDistricts = new SelectList(_context.SubDistricts, "Code", "Name", model.SubDistrictCode);
            return View(model);
        }

        // GET: Edit Governorate
        public IActionResult EditGovernorate(int id)
        {
            var gov = _context.Governorates.Find(id);
            if (gov == null) return NotFound();
            return View(gov);
        }

        // POST: Edit Governorate
        [HttpPost]
        public IActionResult EditGovernorate(Governorate model)
        {
            if (ModelState.IsValid)
            {
                _context.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Governorates");
            }
            return View(model);
        }

        // GET: Delete Governorate
        public IActionResult DeleteGovernorate(int id)
        {
            var gov = _context.Governorates.Find(id);
            if (gov == null) return NotFound();

            _context.Governorates.Remove(gov);
            _context.SaveChanges();
            return RedirectToAction("Governorates");
        }

        [HttpGet]
        public JsonResult GetDistricts(string governorateCode)
        {
            var districts = _context.Districts
                .Where(d => d.GovernorateCode == governorateCode)
                .Select(d => new { d.Code, d.Name })
                .ToList();
            return Json(districts);
        }

        [HttpGet]
        public JsonResult GetSubDistricts(string districtCode)
        {
            var subDistricts = _context.SubDistricts
                .Where(sd => sd.DistrictCode == districtCode)
                .Select(sd => new { sd.Code, sd.Name })
                .ToList();
            return Json(subDistricts);
        }

        [HttpGet]
        public JsonResult GetCommunities(string subDistrictCode)
        {
            var communities = _context.Communities
                .Where(c => c.SubDistrictCode == subDistrictCode)
                .Select(c => new { c.Code, c.Name })
                .ToList();
            return Json(communities);
        }




    }
}
