using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public ProjectController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        // 🔑 Get logged-in employee ID
        private int GetEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new Exception("User not logged in properly.");

            return int.Parse(claim.Value);
        }

        // =========================================
        // GET: Project (🔥 FIXED)
        // =========================================
        public async Task<IActionResult> Index()
        {
            var employeeId = GetEmployeeId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return RedirectToAction("Login", "Account");

            var allProjects = await _context.Projects
                .Include(p => p.Modules)
                .ToListAsync();

            var filteredProjects = new List<Project>();
            var projectPermissions = new Dictionary<int, List<string>>();

            foreach (var p in allProjects)
            {
                bool isAssigned = await _context.Assignments
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == p.ProjectId);

                bool canView = await _permissionService.HasPermission(employeeId, p.ProjectId, "ViewProject");

                if (employee.IsAdmin || (isAssigned && canView))
                {
                    filteredProjects.Add(p);

                    // 🔥 GET ALL PERMISSIONS FOR THIS PROJECT
                    var perms = await _permissionService.GetPermissions(employeeId, p.ProjectId);
                    projectPermissions[p.ProjectId] = perms;
                }
            }

            // 🔥 GLOBAL PERMISSION (CREATE PROJECT)
            ViewBag.CanCreate = await _permissionService.HasPermission(employeeId, null, "CreateProject");

            // 🔥 SEND PROJECT-WISE PERMISSIONS
            ViewBag.ProjectPermissions = projectPermissions;

            return View(filteredProjects);
        }

        // =========================================
        // GET: Project/Details
        // =========================================
        public async Task<IActionResult> Details(int id)
        {
            var employeeId = GetEmployeeId();

            var project = await _context.Projects
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Tasks)
                .Include(p => p.Assignments)
                    .ThenInclude(a => a.Employee)
                .Include(p => p.Assignments)
                    .ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
                return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            // 🔒 ACCESS CHECK
            bool isAssigned = await _context.Assignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == id);

            bool canView = await _permissionService.HasPermission(employeeId, id, "ViewProject");

            if (!employee.IsAdmin && (!isAssigned || !canView))
                return Forbid();

            // =========================================
            // 🔥 PROJECT PERMISSIONS
            // =========================================
            ViewBag.CanEditProject = await _permissionService.HasPermission(employeeId, id, "EditProject");
            ViewBag.CanDeleteProject = await _permissionService.HasPermission(employeeId, id, "DeleteProject");
            ViewBag.CanCreateModule = await _permissionService.HasPermission(employeeId, id, "CreateModule");

            // =========================================
            // 🔥 MODULE PERMISSIONS (PER MODULE)
            // =========================================
            var modulePermissions = new Dictionary<int, List<string>>();

            foreach (var m in project.Modules)
            {
                var perms = await _permissionService.GetPermissions(employeeId, id);
                modulePermissions[m.ModuleId] = perms;
            }

            ViewBag.ModulePermissions = modulePermissions;

            // =========================================
            // 🔥 EMPLOYEE PERMISSIONS
            // =========================================
            ViewBag.CanViewEmployee = await _permissionService.HasPermission(employeeId, id, "ViewAssignment");
            ViewBag.CanEditEmployee = await _permissionService.HasPermission(employeeId, id, "EditAssignment");

            return View(project);
        }

        // =========================================
        // GET: Project/Create
        // =========================================
        public async Task<IActionResult> Create()
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "CreateProject"))
                return Forbid();

            return View();
        }

        // =========================================
        // POST: Project/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "CreateProject"))
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // =========================================
        // GET: Project/Edit
        // =========================================
        public async Task<IActionResult> Edit(int id)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, id, "EditProject"))
                return Forbid();

            var project = await _context.Projects.FindAsync(id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // =========================================
        // POST: Project/Edit
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, id, "EditProject"))
                return Forbid();

            if (id != project.ProjectId)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(project);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // =========================================
        // POST: Project/Delete
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, id, "DeleteProject"))
                return Forbid();

            var project = await _context.Projects
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Tasks)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
                return NotFound();

            // 🔥 CHECK MODULES
            if (project.Modules.Any())
            {
                TempData["Error"] = "⚠️ Please delete all modules before deleting this project.";
                return RedirectToAction("Details", new { id });
            }

            // 🔥 REMOVE ASSIGNMENTS
            var assignments = _context.Assignments
                .Where(a => a.ProjectId == id);

            _context.Assignments.RemoveRange(assignments);

            // 🔥 DELETE PROJECT
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Project deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}