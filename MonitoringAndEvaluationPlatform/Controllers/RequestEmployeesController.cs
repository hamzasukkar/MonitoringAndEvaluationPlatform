using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MonitoringAndEvaluationPlatform.Attributes;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using Microsoft.AspNetCore.Authorization;

namespace MonitoringAndEvaluationPlatform.Controllers
{
    /// <summary>
    /// CRUD for the named people who write requests. They are lookup rows, not
    /// system accounts. Managed by whoever can manage requests.
    /// </summary>
    [Permission(Permissions.ManageRequests)]
    [Authorize]
    public class RequestEmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStringLocalizer<RequestEmployeesController> _localizer;

        public RequestEmployeesController(
            ApplicationDbContext context,
            IStringLocalizer<RequestEmployeesController> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: RequestEmployees
        public async Task<IActionResult> Index()
        {
            var employees = await _context.RequestEmployees
                .Select(e => new EmployeeRow
                {
                    Employee = e,
                    RequestCount = e.Requests.Count,
                    TestCount = e.Tests.Count
                })
                .OrderBy(e => e.Employee.Name)
                .ToListAsync();

            return View(employees);
        }

        // GET: RequestEmployees/Create
        public IActionResult Create()
        {
            return View(new RequestEmployee());
        }

        // POST: RequestEmployees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RequestEmployee employee)
        {
            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            _context.RequestEmployees.Add(employee);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Employee '{0}' has been added."].Value, employee.Name));

            return RedirectToAction(nameof(Index));
        }

        // GET: RequestEmployees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.RequestEmployees.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: RequestEmployees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RequestEmployee employee)
        {
            if (id != employee.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(employee);
            }

            _context.Update(employee);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Employee '{0}' has been updated."].Value, employee.Name));

            return RedirectToAction(nameof(Index));
        }

        // POST: RequestEmployees/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.RequestEmployees.FindAsync(id);
            if (employee == null) return NotFound();

            // Test sign-offs are this employee's own verification record and their FK is
            // Restrict, so deleting here would throw a raw FK violation. Block it with a
            // clear message instead.
            var testCount = await _context.RequestTests.CountAsync(t => t.RequestEmployeeId == id);
            if (testCount > 0)
            {
                this.SetErrorMessage(string.Format(
                    _localizer["'{0}' cannot be deleted: {1} recorded test(s) reference them. Remove those tests first."].Value,
                    employee.Name, testCount));

                return RedirectToAction(nameof(Index));
            }

            // Linked requests are detached automatically (FK is SetNull).
            _context.RequestEmployees.Remove(employee);
            await _context.SaveChangesAsync();

            this.SetSuccessMessage(string.Format(
                _localizer["Employee '{0}' has been deleted."].Value, employee.Name));

            return RedirectToAction(nameof(Index));
        }

        public class EmployeeRow
        {
            public RequestEmployee Employee { get; set; } = default!;
            public int RequestCount { get; set; }
            public int TestCount { get; set; }
        }
    }
}
