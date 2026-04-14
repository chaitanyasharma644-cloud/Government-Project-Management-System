using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public EmployeeController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        // 🔑 Get EmployeeId
        private int GetEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new Exception("User not logged in");

            return int.Parse(claim.Value);
        }

        // =========================================
        // INDEX
        // =========================================
        public async Task<IActionResult> Index(string search)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "ViewEmployee"))
                return Forbid();

            var employees = _context.Employees
                .Include(e => e.Designation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                employees = employees.Where(e =>
                    e.EmployeeName.Contains(search) ||
                    e.Email.Contains(search) ||
                    e.Username.Contains(search));
            }

            ViewBag.CanCreate = await _permissionService.HasPermission(employeeId, null, "CreateEmployee");
            ViewBag.CanEdit = await _permissionService.HasPermission(employeeId, null, "EditEmployee");
            ViewBag.CanDelete = await _permissionService.HasPermission(employeeId, null, "DeleteEmployee");

            return View(await employees.ToListAsync());
        }

        // =========================================
        // CREATE
        // =========================================
        public async Task<IActionResult> Create()
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "CreateEmployee"))
                return Forbid();

            ViewBag.DesignationList = _context.Designations
                .Select(d => new SelectListItem
                {
                    Value = d.DesignationId.ToString(),
                    Text = d.DesignationName
                }).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "CreateEmployee"))
                return Forbid();

            if (ModelState.IsValid)
            {
                employee.Epassword = "nicemployee123#";

                _context.Add(employee);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(employee);
        }

        // =========================================
        // EDIT (GET)
        // =========================================
        public async Task<IActionResult> Edit(int id)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "EditEmployee"))
                return Forbid();

            var emp = await _context.Employees.FindAsync(id);

            if (emp == null)
                return NotFound();

            ViewBag.DesignationList = _context.Designations
                .Select(d => new SelectListItem
                {
                    Value = d.DesignationId.ToString(),
                    Text = d.DesignationName
                }).ToList();

            return View(emp);
        }

        // =========================================
        // EDIT (POST) 🔥 FIXED
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee emp)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "EditEmployee"))
                return Forbid();

            if (id != emp.EmployeeId)
                return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _context.Employees.FindAsync(id);

                if (existing == null)
                    return NotFound();

                // ✅ Update only safe fields
                existing.EmployeeName = emp.EmployeeName;
                existing.Email = emp.Email;
                existing.Username = emp.Username;
                existing.DesignationId = emp.DesignationId;
                existing.IsAdmin = emp.IsAdmin;
                existing.SystemRole = emp.SystemRole;

                // ❗ Password remains unchanged

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(emp);
        }

        // =========================================
        // DELETE
        // =========================================
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "DeleteEmployee"))
                return Forbid();

            var emp = await _context.Employees
                .Include(e => e.Designation)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (emp == null)
                return NotFound();

            return View(emp);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var emp = await _context.Employees.FindAsync(id);

            if (emp != null)
            {
                _context.Employees.Remove(emp);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}