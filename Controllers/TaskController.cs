using GPMS.Data;
using GPMS.Models;
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
        // TASK LIST
        // =========================================
        public async Task<IActionResult> Index(int? projectId, int? moduleId, string search)
        {
            var employeeId = GetEmployeeId();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return Unauthorized();

            var allTasks = await _context.Tasks
                .Include(t => t.Module)
                    .ThenInclude(m => m.Project)
                .ToListAsync();

            var filteredTasks = new List<TaskModel>();
            var taskPermissions = new Dictionary<int, List<string>>();

            foreach (var t in allTasks)
            {
                var projId = t.Module.ProjectId;

                bool isAssigned = await _context.Assignments
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == projId);

                bool canView = await _permissionService.HasPermission(employeeId, projId, "ViewTask");

                if (employee.IsAdmin || (isAssigned && canView))
                {
                    filteredTasks.Add(t);

                    var perms = await _permissionService.GetPermissions(employeeId, projId);
                    taskPermissions[t.TaskId] = perms;
                }
            }

            // Filters
            if (projectId.HasValue)
                filteredTasks = filteredTasks
                    .Where(t => t.Module.ProjectId == projectId.Value)
                    .ToList();

            if (moduleId.HasValue)
                filteredTasks = filteredTasks
                    .Where(t => t.ModuleId == moduleId.Value)
                    .ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                filteredTasks = filteredTasks.Where(t =>
                    t.TaskName.Contains(search) ||
                    (t.TaskDescription != null && t.TaskDescription.Contains(search)) ||
                    (t.TaskStatus != null && t.TaskStatus.Contains(search)) ||
                    (t.Module != null && t.Module.ModuleName.Contains(search))
                ).ToList();
            }

            ViewBag.TaskPermissions = taskPermissions;

            // Project dropdown
            List<Project> projects;
            if (employee.IsAdmin)
            {
                projects = await _context.Projects.ToListAsync();
            }
            else
            {
                var assignedProjectIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ProjectId != null)
                    .Select(a => a.ProjectId.Value)
                    .Distinct()
                    .ToListAsync();

                projects = await _context.Projects
                    .Where(p => assignedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();
            }

            ViewBag.Projects = new SelectList(projects, "ProjectId", "ProjectName", projectId);

            // Module dropdown → only selected project's modules
            if (projectId.HasValue)
            {
                var modules = await _context.Modules
                    .Where(m => m.ProjectId == projectId.Value)
                    .ToListAsync();

                ViewBag.Modules = new SelectList(modules, "ModuleId", "ModuleName", moduleId);
            }
            else
            {
                ViewBag.Modules = new SelectList(new List<Module>(), "ModuleId", "ModuleName");
            }

            ViewBag.SelectedProjectId = projectId;
            ViewBag.SelectedModuleId = moduleId;
            ViewBag.Search = search;

            return View(filteredTasks);
        }

        // =========================================
        // TASK DETAILS
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

            if (employee == null)
                return Unauthorized();

            bool isAssigned = await _context.Assignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == projectId);

            bool canView = await _permissionService.HasPermission(employeeId, projectId, "ViewTask");

            if (!employee.IsAdmin && (!isAssigned || !canView))
                return Forbid();

            ViewBag.CanEditTask = await _permissionService.HasPermission(employeeId, projectId, "EditTask");
            ViewBag.CanDeleteTask = await _permissionService.HasPermission(employeeId, projectId, "DeleteTask");
            ViewBag.CanCreateTask = await _permissionService.HasPermission(employeeId, projectId, "CreateTask");

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

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return Unauthorized();

            if (projectId.HasValue)
            {
                bool canCreate = await _permissionService.HasPermission(employeeId, projectId.Value, "CreateTask");
                if (!employee.IsAdmin && !canCreate)
                    return Forbid();
            }

            List<Project> projects;

            if (employee.IsAdmin)
            {
                projects = await _context.Projects.ToListAsync();
            }
            else
            {
                var assignedProjectIds = await _context.Assignments
                    .Where(a => a.EmployeeId == employeeId && a.ProjectId != null)
                    .Select(a => a.ProjectId.Value)
                    .Distinct()
                    .ToListAsync();

                projects = await _context.Projects
                    .Where(p => assignedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();
            }

            ViewBag.ProjectList = new SelectList(projects, "ProjectId", "ProjectName", projectId);

            if (projectId.HasValue)
            {
                ViewBag.ModuleList = new SelectList(
                    await _context.Modules.Where(m => m.ProjectId == projectId.Value).ToListAsync(),
                    "ModuleId",
                    "ModuleName"
                );
                ViewBag.SelectedProjectId = projectId;
            }
            else
            {
                ViewBag.ModuleList = new SelectList(new List<Module>(), "ModuleId", "ModuleName");
            }

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

            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName", module.ProjectId);
            ViewBag.ModuleList = new SelectList(
                _context.Modules.Where(m => m.ProjectId == module.ProjectId),
                "ModuleId",
                "ModuleName",
                task.ModuleId
            );
            ViewBag.SelectedProjectId = module.ProjectId;

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
            ViewBag.SelectedProjectId = projectId;

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
            ViewBag.ModuleList = new SelectList(
                _context.Modules.Where(m => m.ProjectId == module.ProjectId),
                "ModuleId",
                "ModuleName",
                task.ModuleId
            );
            ViewBag.SelectedProjectId = module.ProjectId;

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

            int assignmentCount = await _context.Assignments
                .CountAsync(a => a.TaskId == id);

            if (assignmentCount > 0)
            {
                TempData["Error"] = $"Cannot delete task. It has {assignmentCount} assignments. Remove them first.";
                return RedirectToAction("Details", new { id });
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Task deleted successfully.";

            return RedirectToAction(nameof(Index), new { projectId });
        }

        // =========================================
        // AJAX: MODULES BY PROJECT
        // =========================================
        [HttpGet]
        public JsonResult GetModulesByProject(int projectId)
        {
            var modules = _context.Modules
                .Where(m => m.ProjectId == projectId)
                .Select(m => new
                {
                    moduleId = m.ModuleId,
                    moduleName = m.ModuleName
                })
                .ToList();

            return Json(modules);
        }
    }
}