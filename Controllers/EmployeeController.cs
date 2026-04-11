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

            // 🔒 VIEW PERMISSION
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
                    e.Username.Contains(search)
                );
            }

            // 🔥 UI Permissions
            ViewBag.CanCreate = await _permissionService.HasPermission(employeeId, null, "CreateEmployee");
            ViewBag.CanEdit = await _permissionService.HasPermission(employeeId, null, "EditEmployee");
            ViewBag.CanDelete = await _permissionService.HasPermission(employeeId, null, "DeleteEmployee");

            return View(await employees.ToListAsync());
        }

        // =========================================
        // CREATE (GET)
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

        // =========================================
        // CREATE (POST)
        // =========================================
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

                TempData["Success"] = "✅ Employee created successfully.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.DesignationList = _context.Designations
                .Select(d => new SelectListItem
                {
                    Value = d.DesignationId.ToString(),
                    Text = d.DesignationName
                }).ToList();

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

            ViewBag.Designations = await _context.Designations.ToListAsync();

            return View(emp);
        }

        // =========================================
        // EDIT (POST)
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
                try
                {
                    _context.Update(emp);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "✅ Employee updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Employees.Any(e => e.EmployeeId == emp.EmployeeId))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(emp);
        }

        // =========================================
        // DELETE (GET)
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

        // =========================================
        // DELETE (POST)
        // =========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "DeleteEmployee"))
                return Forbid();

            var emp = await _context.Employees.FindAsync(id);

            if (emp != null)
            {
                _context.Employees.Remove(emp);
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Employee deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}