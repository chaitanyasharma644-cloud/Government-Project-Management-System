using GPMS.Data;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// ALIAS
using TaskModel = GPMS.Models.Task;

namespace GPMS.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public TaskController(AppDbContext context, PermissionService permissionService)
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
        // TASK LIST (🔥 UPDATED)
        // =========================================
        public async Task<IActionResult> Index(int? projectId, int? moduleId, string search)
        {
            var employeeId = GetEmployeeId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            var allTasks = await _context.Tasks
                .Include(t => t.Module)
                    .ThenInclude(m => m.Project)
                .ToListAsync();

            var filteredTasks = new List<TaskModel>();
            var taskPermissions = new Dictionary<int, List<string>>(); // ✅ ADDED

            foreach (var t in allTasks)
            {
                var projId = t.Module.ProjectId;

                bool isAssigned = await _context.Assignments
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == projId);

                bool canView = await _permissionService.HasPermission(employeeId, projId, "ViewTask");

                if (employee.IsAdmin || (isAssigned && canView))
                {
                    filteredTasks.Add(t);

                    // ✅ ADD PER-TASK PERMISSIONS
                    var perms = await _permissionService.GetPermissions(employeeId, projId);
                    taskPermissions[t.TaskId] = perms;
                }
            }

            // 🔍 Filters
            if (projectId.HasValue)
                filteredTasks = filteredTasks.Where(t => t.Module.ProjectId == projectId).ToList();

            if (moduleId.HasValue)
                filteredTasks = filteredTasks.Where(t => t.ModuleId == moduleId).ToList();

            if (!string.IsNullOrEmpty(search))
                filteredTasks = filteredTasks.Where(t => t.TaskName.Contains(search)).ToList();

            ViewBag.TaskPermissions = taskPermissions; // ✅ ADDED

            ViewBag.Projects = new SelectList(_context.Projects, "ProjectId", "ProjectName", projectId);
            ViewBag.Modules = new SelectList(_context.Modules, "ModuleId", "ModuleName", moduleId);

            return View(filteredTasks);
        }

        // =========================================
        // TASK DETAILS (🔥 UPDATED)
        // =========================================
        public async Task<IActionResult> Details(int id)
        {
            var employeeId = GetEmployeeId();

            var task = await _context.Tasks
                .Include(t => t.Module)
                    .ThenInclude(m => m.Project)
                .Include(t => t.Assignments)
                    .ThenInclude(a => a.Employee)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
                return NotFound();

            var projectId = task.Module.ProjectId;

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            bool isAssigned = await _context.Assignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == projectId);

            bool canView = await _permissionService.HasPermission(employeeId, projectId, "ViewTask");

            if (!employee.IsAdmin && (!isAssigned || !canView))
                return Forbid();

            // 🔥 UI permissions
            ViewBag.CanEditTask = await _permissionService.HasPermission(employeeId, projectId, "EditTask"); // ✅ FIXED NAME
            ViewBag.CanDeleteTask = await _permissionService.HasPermission(employeeId, projectId, "DeleteTask"); // ✅ FIXED NAME
            ViewBag.CanCreateTask = await _permissionService.HasPermission(employeeId, projectId, "CreateTask"); // ✅ FIXED NAME

            // ✅ ADD EMPLOYEE PERMISSIONS (required by view)
            ViewBag.CanViewEmployee = await _permissionService.HasPermission(employeeId, projectId, "ViewAssignment");
            ViewBag.CanEditEmployee = await _permissionService.HasPermission(employeeId, projectId, "EditAssignment");

            return View(task);
        }

        // =========================================
        // CREATE TASK (GET)
        // =========================================
        public async Task<IActionResult> Create(int? projectId)
        {
            var employeeId = GetEmployeeId();

            if (projectId == null || !await _permissionService.HasPermission(employeeId, projectId, "CreateTask"))
                return Forbid();

            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName", projectId);

            ViewBag.ModuleList = new SelectList(
                _context.Modules.Where(m => m.ProjectId == projectId),
                "ModuleId",
                "ModuleName"
            );

            return View();
        }

        // =========================================
        // CREATE TASK (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskModel task)
        {
            var employeeId = GetEmployeeId();

            var module = await _context.Modules.FindAsync(task.ModuleId);

            if (module == null)
                return NotFound();

            if (!await _permissionService.HasPermission(employeeId, module.ProjectId, "CreateTask"))
                return Forbid();

            if (ModelState.IsValid)
            {
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Task created successfully.";
                return RedirectToAction(nameof(Index), new { projectId = module.ProjectId });
            }

            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            ViewBag.ModuleList = new SelectList(_context.Modules, "ModuleId", "ModuleName");

            return View(task);
        }

        // =========================================
        // EDIT TASK (GET)
        // =========================================
        public async Task<IActionResult> Edit(int id)
        {
            var employeeId = GetEmployeeId();

            var task = await _context.Tasks
                .Include(t => t.Module)
                    .ThenInclude(m => m.Project)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
                return NotFound();

            var projectId = task.Module.ProjectId;

            if (!await _permissionService.HasPermission(employeeId, projectId, "EditTask"))
                return Forbid();

            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName", projectId);
            ViewBag.ModuleList = new SelectList(
                _context.Modules.Where(m => m.ProjectId == projectId),
                "ModuleId",
                "ModuleName",
                task.ModuleId
            );

            return View(task);
        }

        // =========================================
        // EDIT TASK (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskModel task)
        {
            var employeeId = GetEmployeeId();

            var module = await _context.Modules.FindAsync(task.ModuleId);

            if (module == null)
                return NotFound();

            if (!await _permissionService.HasPermission(employeeId, module.ProjectId, "EditTask"))
                return Forbid();

            if (ModelState.IsValid)
            {
                var existingTask = await _context.Tasks.FindAsync(task.TaskId);

                if (existingTask == null)
                    return NotFound();

                existingTask.TaskName = task.TaskName;
                existingTask.TaskDescription = task.TaskDescription;
                existingTask.TaskStatus = task.TaskStatus;
                existingTask.TaskEndDate = task.TaskEndDate;
                existingTask.ModuleId = task.ModuleId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "✅ Task updated successfully.";
                return RedirectToAction(nameof(Index), new { projectId = module.ProjectId });
            }

            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName", module.ProjectId);
            ViewBag.ModuleList = new SelectList(_context.Modules, "ModuleId", "ModuleName", task.ModuleId);

            return View(task);
        }

        // =========================================
        // DELETE TASK
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = GetEmployeeId();

            var task = await _context.Tasks
                .Include(t => t.Module)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
                return NotFound();

            var projectId = task.Module.ProjectId;

            if (!await _permissionService.HasPermission(employeeId, projectId, "DeleteTask"))
                return Forbid();

            var assignments = _context.Assignments
                .Where(a => a.TaskId == id);

            _context.Assignments.RemoveRange(assignments);

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Task deleted successfully.";

            return RedirectToAction(nameof(Index), new { projectId });
        }

        // =========================================
        // AJAX: MODULES BY PROJECT
        // =========================================
        public JsonResult GetModulesByProject(int projectId)
        {
            var modules = _context.Modules
                .Where(m => m.ProjectId == projectId)
                .Select(m => new
                {
                    ModuleId = m.ModuleId,
                    ModuleName = m.ModuleName
                })
                .ToList();

            return Json(modules);
        }
    }
}