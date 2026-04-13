using GPMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 🔑 Get logged-in employee ID safely
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                return RedirectToAction("Login", "Account");

            var employeeId = int.Parse(claim.Value);

            // 🔍 Get employee
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return RedirectToAction("Login", "Account");

            // =====================================================
            // 👑 ADMIN DASHBOARD
            // =====================================================
            if (employee.IsAdmin)
            {
                ViewBag.IsAdmin = true;

                ViewBag.ProjectCount = await _context.Projects.CountAsync();
                ViewBag.ModuleCount = await _context.Modules.CountAsync();
                ViewBag.TaskCount = await _context.Tasks.CountAsync();
                ViewBag.EmployeeCount = await _context.Employees.CountAsync();

                ViewBag.Completed = await _context.Tasks
                    .CountAsync(t => t.TaskStatus == "Completed");

                ViewBag.Ongoing = await _context.Tasks
                    .CountAsync(t => t.TaskStatus == "Ongoing");
            }
            else
            {
                // =====================================================
                // 👤 EMPLOYEE DASHBOARD
                // =====================================================
                ViewBag.IsAdmin = false;

                // 🔹 PROJECTS (assigned)
                var projectIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ProjectId != null)
                    .Select(a => a.ProjectId)
                    .Distinct()
                    .ToListAsync();

                // 🔹 MODULES (assigned)
                var moduleIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ModuleId != null)
                    .Select(a => a.ModuleId)
                    .Distinct()
                    .ToListAsync();

                // 🔹 TASKS (assigned)
                var taskIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.TaskId != null)
                    .Select(a => a.TaskId)
                    .Distinct()
                    .ToListAsync();

                ViewBag.ProjectCount = projectIds.Count;
                ViewBag.ModuleCount = moduleIds.Count;
                ViewBag.TaskCount = taskIds.Count;

                // 🔹 TASK STATUS (based on assigned projects)
                var tasks = _context.Tasks
                    .Include(t => t.Module)
                    .Where(t => projectIds.Contains(t.Module.ProjectId));

                ViewBag.Ongoing = await tasks
                    .CountAsync(t => t.TaskStatus == "Ongoing");

                ViewBag.Completed = await tasks
                    .CountAsync(t => t.TaskStatus == "Completed");
            }

            return View();
        }
    }
}