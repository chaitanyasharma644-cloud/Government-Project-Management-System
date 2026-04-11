using GPMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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

        public IActionResult Index()
        {
            // 🔑 Get logged-in employee ID
            var employeeId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // 🔐 Check role
            if (User.IsInRole("Admin"))
            {
                // =============================
                // 👑 ADMIN DASHBOARD
                // =============================
                ViewBag.Projects = _context.Projects.Count();
                ViewBag.Modules = _context.Modules.Count();
                ViewBag.Tasks = _context.Tasks.Count();
                ViewBag.Assignments = _context.Assignments.Count();
                ViewBag.Employees = _context.Employees.Count();

                ViewBag.Completed = _context.Projects
                    .Where(p => p.ProjectStatus == "Completed")
                    .Count();

                ViewBag.Ongoing = _context.Projects
                    .Where(p => p.ProjectStatus == "Ongoing")
                    .Count();
            }
            else
            {
                // =============================
                // 👤 EMPLOYEE DASHBOARD
                // =============================

                // 🔹 PROJECTS (assigned)
                var projectIds = _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ProjectId != null)
                    .Select(a => a.ProjectId)
                    .Distinct()
                    .ToList();

                ViewBag.Projects = projectIds.Count;

                // 🔹 MODULES (assigned)
                var moduleIds = _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ModuleId != null)
                    .Select(a => a.ModuleId)
                    .Distinct()
                    .ToList();

                ViewBag.Modules = moduleIds.Count;

                // 🔹 TASKS (assigned)
                var taskIds = _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.TaskId != null)
                    .Select(a => a.TaskId)
                    .Distinct()
                    .ToList();

                ViewBag.Tasks = taskIds.Count;

                // 🔹 ASSIGNMENTS
                ViewBag.Assignments = _context.Assignments
                    .Count(a => a.EmployeeId == employeeId);

                // 🔹 EMPLOYEES (hide or keep 0)
                ViewBag.Employees = 0;

                // 🔹 PROJECT STATUS (only assigned projects)
                ViewBag.Completed = _context.Projects
                    .Where(p => projectIds.Contains(p.ProjectId) && p.ProjectStatus == "Completed")
                    .Count();

                ViewBag.Ongoing = _context.Projects
                    .Where(p => projectIds.Contains(p.ProjectId) && p.ProjectStatus == "Ongoing")
                    .Count();
            }

            return View();
        }
    }
}