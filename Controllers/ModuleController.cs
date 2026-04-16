using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [Authorize]
    public class ModuleController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public ModuleController(AppDbContext context, PermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        private int GetEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
                throw new Exception("User not logged in");

            return int.Parse(claim.Value);
        }

        // =========================================
        // GET: Module/Index (🔥 UPDATED)
        // =========================================
        public async Task<IActionResult> Index()
        {
            var employeeId = GetEmployeeId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return RedirectToAction("Login", "Account");

            var allModules = await _context.Modules
                .Include(m => m.Project)
                .Include(m => m.Tasks)
                .ToListAsync();

            var filteredModules = new List<Module>();
            var modulePermissions = new Dictionary<int, List<string>>(); // ✅ ADDED

            foreach (var m in allModules)
            {
                bool isAssigned = await _context.Assignments
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == m.ProjectId);

                bool canView = await _permissionService.HasPermission(employeeId, m.ProjectId, "ViewModule");

                if (employee.IsAdmin || (isAssigned && canView))
                {
                    filteredModules.Add(m);

                    // ✅ ADD PER-MODULE PERMISSIONS
                    var perms = await _permissionService.GetPermissions(employeeId, m.ProjectId);
                    modulePermissions[m.ModuleId] = perms;
                }
            }

            ViewBag.ModulePermissions = modulePermissions; // ✅ ADDED

            // 🔥 GLOBAL UI (only for CREATE)
            ViewBag.CanCreateModule = await _permissionService.HasPermission(employeeId, null, "CreateModule");

            ViewBag.Projects = await _context.Projects.ToListAsync();

            return View(filteredModules);
        }

        // =========================================
        // GET: Module/Details (🔥 UPDATED)
        // =========================================
        public async Task<IActionResult> Details(int id)
        {
            var employeeId = GetEmployeeId();

            var module = await _context.Modules
                .Include(m => m.Project)
                .Include(m => m.Tasks)
                .Include(m => m.Assignments)
                    .ThenInclude(a => a.Employee)
                .Include(m => m.Assignments)
                    .ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(m => m.ModuleId == id);

            if (module == null)
                return NotFound();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            bool isAssigned = await _context.Assignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == module.ProjectId);

            bool canView = await _permissionService.HasPermission(employeeId, module.ProjectId, "ViewModule");

            if (!employee.IsAdmin && (!isAssigned || !canView))
                return Forbid();

            // 🔥 TASK PERMISSIONS (existing)
            ViewBag.CanViewTask = await _permissionService.HasPermission(employeeId, module.ProjectId, "ViewTask");
            ViewBag.CanEditTask = await _permissionService.HasPermission(employeeId, module.ProjectId, "EditTask");
            ViewBag.CanDeleteTask = await _permissionService.HasPermission(employeeId, module.ProjectId, "DeleteTask");
            ViewBag.CanCreateTask = await _permissionService.HasPermission(employeeId, module.ProjectId, "CreateTask");

            // 🔥 EMPLOYEE PERMISSIONS (existing)
            ViewBag.CanViewEmployee = await _permissionService.HasPermission(employeeId, module.ProjectId, "ViewAssignment");
            ViewBag.CanEditEmployee = await _permissionService.HasPermission(employeeId, module.ProjectId, "EditAssignment");

            // ✅ ADD PER-TASK PERMISSIONS
            var taskPermissions = new Dictionary<int, List<string>>();

            foreach (var t in module.Tasks)
            {
                var perms = await _permissionService.GetPermissions(employeeId, module.ProjectId);
                taskPermissions[t.TaskId] = perms;
            }

            ViewBag.TaskPermissions = taskPermissions; // ✅ ADDED

            return View(module);
        }

        // =========================================
        // GET: Module/Create
        // =========================================
        public async Task<IActionResult> Create()
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "CreateModule"))
                return Forbid();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            List<Project> projects;

            if (employee != null && employee.IsAdmin)
            {
                // Admin sees all projects
                projects = await _context.Projects.ToListAsync();
            }
            else
            {
                // Employee sees only their assigned projects
                var assignedProjectIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId)
                    .Select(a => a.ProjectId)
                    .Distinct()
                    .ToListAsync();

                projects = await _context.Projects
                    .Where(p => assignedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();
            }

            ViewBag.ProjectList = new SelectList(projects, "ProjectId", "ProjectName");

            return View();
        }

        // =========================================
        // POST: Module/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Module module)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, module.ProjectId, "CreateModule"))
                return Forbid();

            if (module.ProjectId == 0)
                ModelState.AddModelError("", "Project is required");

            if (ModelState.IsValid)
            {
                _context.Modules.Add(module);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Module created successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Re-populate dropdown on validation failure
            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            return View(module);
        }

        // =========================================
        // GET: Module/Edit
        // =========================================
        public async Task<IActionResult> Edit(int id)
        {
            var employeeId = GetEmployeeId();

            var module = await _context.Modules.FindAsync(id);

            if (module == null)
                return NotFound();

            if (!await _permissionService.HasPermission(employeeId, module.ProjectId, "EditModule"))
                return Forbid();

            ViewBag.Projects = new SelectList(_context.Projects, "ProjectId", "ProjectName", module.ProjectId);

            return View(module);
        }

        // =========================================
        // POST: Module/Edit
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Module module)
        {
            var employeeId = GetEmployeeId();

            if (id != module.ModuleId)
                return NotFound();

            if (!await _permissionService.HasPermission(employeeId, module.ProjectId, "EditModule"))
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.Update(module);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Module updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Projects = new SelectList(_context.Projects, "ProjectId", "ProjectName", module.ProjectId);

            return View(module);
        }

        // =========================================
        // POST: Module/Delete
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = GetEmployeeId();

            var module = await _context.Modules
                .Include(m => m.Tasks)
                .FirstOrDefaultAsync(m => m.ModuleId == id);

            if (module == null)
                return NotFound();

            if (!await _permissionService.HasPermission(employeeId, module.ProjectId, "DeleteModule"))
                return Forbid();

            // 🔥 CHECK TASKS
            if (module.Tasks.Any())
            {
                TempData["Error"] = $"Cannot delete module. It has {module.Tasks.Count} tasks. Delete them first.";
                return RedirectToAction("Details", "Project", new { id = module.ProjectId });
            }

            // 🔥 CHECK ASSIGNMENTS (OPTIMIZED)
            int assignmentCount = await _context.Assignments
                .CountAsync(a => a.ModuleId == id);

            if (assignmentCount > 0)
            {
                TempData["Error"] = $"Cannot delete module. It has {assignmentCount} assignments. Remove them first.";
                return RedirectToAction("Details", "Project", new { id = module.ProjectId });
            }

            // ✅ SAFE DELETE
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Module deleted successfully.";

            return RedirectToAction("Details", "Project", new { id = module.ProjectId });
        }
    }
}