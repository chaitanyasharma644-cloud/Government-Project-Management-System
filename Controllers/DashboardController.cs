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
            // 🔑 Get logged-in employee ID
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
            // 👑 ADMIN DASHBOARD (ALL PROJECTS)
            // =====================================================
            if (employee.IsAdmin)
            {
                ViewBag.IsAdmin = true;

                ViewBag.ProjectCount = await _context.Projects.CountAsync();
                ViewBag.ModuleCount = await _context.Modules.CountAsync();
                ViewBag.EmployeeCount = await _context.Employees.CountAsync();

                // 🔹 PROJECT STATUS (ALL PROJECTS)
                ViewBag.CompletedProjects = await _context.Projects
                    .CountAsync(p => p.ProjectStatus == "Completed");

                ViewBag.OngoingProjects = await _context.Projects
                    .CountAsync(p => p.ProjectStatus == "Ongoing");
            }
            else
            {
                // =====================================================
                // 👤 EMPLOYEE DASHBOARD (ASSIGNED PROJECTS ONLY)
                // =====================================================
                ViewBag.IsAdmin = false;

                // 🔹 ASSIGNED PROJECTS
                var projectIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ProjectId != null)
                    .Select(a => a.ProjectId.Value)
                    .Distinct()
                    .ToListAsync();

                // 🔹 ASSIGNED MODULES
                var moduleIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ModuleId != null)
                    .Select(a => a.ModuleId.Value)
                    .Distinct()
                    .ToListAsync();

                ViewBag.ProjectCount = projectIds.Count;
                ViewBag.ModuleCount = moduleIds.Count;

                // 🔹 PROJECT STATUS (ONLY ASSIGNED PROJECTS)
                ViewBag.CompletedProjects = await _context.Projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .CountAsync(p => p.ProjectStatus == "Completed");

                ViewBag.OngoingProjects = await _context.Projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .CountAsync(p => p.ProjectStatus == "Ongoing");
            }

            return View();
        }
    }
}