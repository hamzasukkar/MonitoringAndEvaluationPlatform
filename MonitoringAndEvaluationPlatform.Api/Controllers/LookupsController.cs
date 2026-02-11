using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Dtos;

namespace MonitoringAndEvaluationPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LookupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LookupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("governorates")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemDto>>> GetGovernorates()
        {
            var items = await _context.Governorates
                .Select(g => new LookupItemDto
                {
                    Code = g.Code,
                    Name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? g.AR_Name : g.EN_Name,
                    NameAr = g.AR_Name,
                    NameEn = g.EN_Name
                }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("districts")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemDto>>> GetDistricts([FromQuery] string? governorateCode = null)
        {
            var query = _context.Districts.AsQueryable();
            if (!string.IsNullOrEmpty(governorateCode))
                query = query.Where(d => d.GovernorateCode == governorateCode);

            var items = await query
                .Select(d => new LookupItemDto
                {
                    Code = d.Code,
                    Name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? d.AR_Name : d.EN_Name,
                    NameAr = d.AR_Name,
                    NameEn = d.EN_Name
                }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("subdistricts")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemDto>>> GetSubDistricts([FromQuery] string? districtCode = null)
        {
            var query = _context.SubDistricts.AsQueryable();
            if (!string.IsNullOrEmpty(districtCode))
                query = query.Where(s => s.DistrictCode == districtCode);

            var items = await query
                .Select(s => new LookupItemDto
                {
                    Code = s.Code,
                    Name = s.AR_Name,
                    NameAr = s.AR_Name
                }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("communities")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemDto>>> GetCommunities([FromQuery] string? subDistrictCode = null)
        {
            var query = _context.Communities.AsQueryable();
            if (!string.IsNullOrEmpty(subDistrictCode))
                query = query.Where(c => c.SubDistrictCode == subDistrictCode);

            var items = await query
                .Select(c => new LookupItemDto
                {
                    Code = c.Code,
                    Name = c.AR_Name,
                    NameAr = c.AR_Name
                }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("ministries")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemIntDto>>> GetMinistries()
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            var items = await _context.Ministries
                .Select(m => new LookupItemIntDto
                {
                    Code = m.Code,
                    Name = isArabic ? m.MinistryDisplayName_AR : m.MinistryDisplayName_EN,
                    NameAr = m.MinistryDisplayName_AR,
                    NameEn = m.MinistryDisplayName_EN
                }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("sectors")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemIntDto>>> GetSectors()
        {
            var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
            var items = await _context.Sectors
                .Select(s => new LookupItemIntDto
                {
                    Code = s.Code,
                    Name = isArabic ? s.AR_Name : s.EN_Name,
                    NameAr = s.AR_Name,
                    NameEn = s.EN_Name
                }).ToListAsync();
            return Ok(items);
        }

        [HttpGet("donors")]
        [ResponseCache(Duration = 300)]
        public async Task<ActionResult<List<LookupItemIntDto>>> GetDonors()
        {
            var items = await _context.Donors
                .Select(d => new LookupItemIntDto
                {
                    Code = d.Code,
                    Name = d.Partner
                }).ToListAsync();
            return Ok(items);
        }
    }
}
