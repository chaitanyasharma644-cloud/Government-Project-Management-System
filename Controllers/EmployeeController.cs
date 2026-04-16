using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;
        private readonly IPasswordHasher<Employee> _passwordHasher;

        public EmployeeController(
            AppDbContext context,
            PermissionService permissionService,
            IPasswordHasher<Employee> passwordHasher)
        {
            _context = context;
            _permissionService = permissionService;
            _passwordHasher = passwordHasher;
        }

        // 🔑 Get Logged-in EmployeeId
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

            // UI Permissions
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

            await LoadDesignations();

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
                var defaultPassword = "nicemployee123#";

                // 🔐 Hash password
                employee.Epassword = _passwordHasher.HashPassword(employee, defaultPassword);

                // 🆕 First login setup
                employee.IsFirstLogin = true;
                employee.PasswordChangedAt = null;

                _context.Add(employee);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Employee created successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadDesignations();
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

            await LoadDesignations();

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
                    var existing = await _context.Employees.FindAsync(id);

                    if (existing == null)
                        return NotFound();

                    // ✅ Update only safe fields
                    existing.EmployeeName = emp.EmployeeName;
                    existing.Email = emp.Email;
                    existing.Username = emp.Username;
                    existing.DesignationId = emp.DesignationId;
                    existing.SystemRole = emp.SystemRole;
                    existing.IsAdmin = emp.IsAdmin;

                    // ❌ Do NOT touch password-related fields

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Employee updated successfully.";
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

            await LoadDesignations();
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

                TempData["Success"] = "Employee deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================
        // 🔁 HELPER: Load Designations Dropdown
        // =========================================
        private async System.Threading.Tasks.Task LoadDesignations()
        {
            ViewBag.DesignationList = await _context.Designations
                .Select(d => new SelectListItem
                {
                    Value = d.DesignationId.ToString(),
                    Text = d.DesignationName
                }).ToListAsync();
        }
    }
}