using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using GPMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPMS.Controllers
{
    [Authorize]
    public class AssignmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PermissionService _permissionService;

        public AssignmentController(AppDbContext context, PermissionService permissionService)
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
        // 🔐 CORE ACCESS CHECK (🔥 IMPORTANT)
        // =========================================
        private async Task<bool> HasAccess(int employeeId, int projectId, string permission)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee.IsAdmin)
                return true;

            bool isAssigned = await _context.Assignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == projectId);

            bool hasPermission = await _permissionService.HasPermission(employeeId, projectId, permission);

            return isAssigned && hasPermission;
        }

        private async Task<bool> HasEditPermission(int employeeId, int projectId, string type)
        {
            return type.ToLower() switch
            {
                "project" => await HasAccess(employeeId, projectId, "EditProject"),
                "module" => await HasAccess(employeeId, projectId, "EditModule"),
                "task" => await HasAccess(employeeId, projectId, "EditTask"),
                _ => false
            };
        }

        // =========================================
        // 🔹 ASSIGN EMPLOYEE (GET)
        // =========================================
        public async Task<IActionResult> AssignEmployee(int id, string type)
        {
            var employeeId = GetEmployeeId();

            int projectId = await ResolveProjectId(id, type);
            if (projectId == 0) return NotFound();

            if (!await HasEditPermission(employeeId, projectId, type))
                return Forbid();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            // 🔥 FILTER PROJECTS
            var projects = await _context.Projects
                .Where(p =>
                    employee.IsAdmin ||
                    (_context.Assignments.Any(a => a.EmployeeId == employeeId && a.ProjectId == p.ProjectId)
                     && _permissionService.HasPermission(employeeId, p.ProjectId, "EditProject").Result))
                .ToListAsync();

            // 🔥 FILTER MODULES
            var modules = await _context.Modules
                .Where(m =>
                    employee.IsAdmin ||
                    (_context.Assignments.Any(a => a.EmployeeId == employeeId && a.ProjectId == m.ProjectId)
                     && _permissionService.HasPermission(employeeId, m.ProjectId, "EditModule").Result))
                .ToListAsync();

            // 🔥 FILTER TASKS
            var tasks = await _context.Tasks
                .Include(t => t.Module)
                .Where(t =>
                    employee.IsAdmin ||
                    (_context.Assignments.Any(a => a.EmployeeId == employeeId && a.ProjectId == t.Module.ProjectId)
                     && _permissionService.HasPermission(employeeId, t.Module.ProjectId, "EditTask").Result))
                .ToListAsync();

            var model = new AssignmentViewModel
            {
                Employees = new SelectList(_context.Employees, "EmployeeId", "EmployeeName"),
                Projects = new SelectList(projects, "ProjectId", "ProjectName"),
                Modules = new SelectList(modules, "ModuleId", "ModuleName"),
                Tasks = new SelectList(tasks, "TaskId", "TaskName"),
                Roles = new SelectList(_context.Roles, "RoleId", "RoleName"),
                CurrentLevel = type,
                AssignedDate = DateTime.Now,

                // 🔥 PRE-SELECT BASED ON LEVEL
                ProjectId = type == "project" ? id : (int?)projectId,
                ModuleId = type == "module" ? id : null,
                TaskId = type == "task" ? id : null
            };

            return View(model);
        }

        // =========================================
        // 🔹 ASSIGN EMPLOYEE (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEmployee(AssignmentViewModel model)
        {
            var employeeId = GetEmployeeId();

            int projectId = await ResolveProjectIdFromModel(model);
            if (projectId == 0) return NotFound();

            if (!await HasEditPermission(employeeId, projectId, model.CurrentLevel))
                return Forbid();

            if (model.EmployeeId == 0)
                ModelState.AddModelError("EmployeeId", "Please select an employee.");

            if (model.RoleId == 0)
                ModelState.AddModelError("RoleId", "Please select a role.");

            bool alreadyAssigned = await _context.Assignments.AnyAsync(a =>
                a.EmployeeId == model.EmployeeId &&
                a.ProjectId == model.ProjectId &&
                a.ModuleId == model.ModuleId &&
                a.TaskId == model.TaskId
            );

            if (alreadyAssigned)
                ModelState.AddModelError("", "This employee is already assigned here.");

            if (!ModelState.IsValid)
            {
                ReloadDropdowns(model);
                return View(model);
            }

            var assignment = new Assignment
            {
                EmployeeId = model.EmployeeId,
                RoleId = model.RoleId,
                AssignedDate = DateOnly.FromDateTime(model.AssignedDate),
                ProjectId = model.ProjectId,
                ModuleId = model.ModuleId,
                TaskId = model.TaskId
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment added successfully.";

            return RedirectToEntity(assignment);
        }

        // =========================================
        // 🔹 EDIT ASSIGNMENT
        // =========================================
        public async Task<IActionResult> EditAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
                return NotFound();

            int projectId = await ResolveProjectIdFromAssignment(assignment);
            if (projectId == 0) return NotFound();

            var employeeId = GetEmployeeId();

            if (!await HasEditPermission(employeeId, projectId, GetTypeFromAssignment(assignment)))
                return Forbid();

            ViewBag.EmployeeList = new SelectList(_context.Employees, "EmployeeId", "EmployeeName", assignment.EmployeeId);
            ViewBag.RoleList = new SelectList(_context.Roles, "RoleId", "RoleName", assignment.RoleId);

            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(Assignment model)
        {
            var assignment = await _context.Assignments.FindAsync(model.AssignmentId);
            if (assignment == null)
                return NotFound();

            int projectId = await ResolveProjectIdFromAssignment(assignment);
            if (projectId == 0) return NotFound();

            var employeeId = GetEmployeeId();

            if (!await HasEditPermission(employeeId, projectId, GetTypeFromAssignment(assignment)))
                return Forbid();

            assignment.EmployeeId = model.EmployeeId;
            assignment.RoleId = model.RoleId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment updated successfully.";

            return RedirectToEntity(assignment);
        }

        // =========================================
        // 🔹 DELETE ASSIGNMENT
        // =========================================
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
                return NotFound();

            int projectId = await ResolveProjectIdFromAssignment(assignment);
            if (projectId == 0) return NotFound();

            var employeeId = GetEmployeeId();

            if (!await HasEditPermission(employeeId, projectId, GetTypeFromAssignment(assignment)))
                return Forbid();

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Assignment removed successfully.";

            return RedirectToEntity(assignment);
        }

        // =========================================
        // 🔧 HELPERS (UNCHANGED)
        // =========================================

        private async Task<int> ResolveProjectId(int id, string type)
        {
            return type.ToLower() switch
            {
                "project" => id,
                "module" => (await _context.Modules.FindAsync(id))?.ProjectId ?? 0,
                "task" => (await _context.Tasks.Include(t => t.Module).FirstOrDefaultAsync(t => t.TaskId == id))?.Module.ProjectId ?? 0,
                _ => 0
            };
        }

        private async Task<int> ResolveProjectIdFromModel(AssignmentViewModel model)
        {
            if (model.ProjectId.HasValue)
                return model.ProjectId.Value;

            if (model.ModuleId.HasValue)
                return (await _context.Modules.FindAsync(model.ModuleId))?.ProjectId ?? 0;

            if (model.TaskId.HasValue)
            {
                var task = await _context.Tasks.Include(t => t.Module)
                    .FirstOrDefaultAsync(t => t.TaskId == model.TaskId);

                return task?.Module.ProjectId ?? 0;
            }

            return 0;
        }

        private async Task<int> ResolveProjectIdFromAssignment(Assignment assignment)
        {
            if (assignment.ProjectId != null)
                return assignment.ProjectId.Value;

            if (assignment.ModuleId != null)
                return (await _context.Modules.FindAsync(assignment.ModuleId))?.ProjectId ?? 0;

            if (assignment.TaskId != null)
            {
                var task = await _context.Tasks.Include(t => t.Module)
                    .FirstOrDefaultAsync(t => t.TaskId == assignment.TaskId);

                return task?.Module.ProjectId ?? 0;
            }

            return 0;
        }

        private string GetTypeFromAssignment(Assignment a)
        {
            if (a.TaskId != null) return "task";
            if (a.ModuleId != null) return "module";
            return "project";
        }

        private IActionResult RedirectToEntity(Assignment a)
        {
            if (a.ProjectId != null)
                return RedirectToAction("Details", "Project", new { id = a.ProjectId });

            if (a.ModuleId != null)
                return RedirectToAction("Details", "Module", new { id = a.ModuleId });

            if (a.TaskId != null)
                return RedirectToAction("Details", "Task", new { id = a.TaskId });

            return RedirectToAction("Index", "Project");
        }

        private void ReloadDropdowns(AssignmentViewModel model)
        {
            model.Employees = new SelectList(_context.Employees, "EmployeeId", "EmployeeName");
            model.Projects = new SelectList(_context.Projects, "ProjectId", "ProjectName");
            model.Modules = new SelectList(_context.Modules, "ModuleId", "ModuleName");
            model.Tasks = new SelectList(_context.Tasks, "TaskId", "TaskName");
            model.Roles = new SelectList(_context.Roles, "RoleId", "RoleName");
        }
    }
}