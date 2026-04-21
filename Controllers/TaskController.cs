using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
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
        // 🔥 UPDATED INDEX (ADDED FILTERS)
        // =========================================
        public async Task<IActionResult> Index(
            int? projectId,
            int? moduleId,
            string search,
            DateTime? startDate,
            DateTime? endDate,
            string status)
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
                    bool match = true;

                    // 🔥 NEW DATE FILTERS (DateOnly FIX)
                    if (startDate.HasValue)
                    {
                        var start = DateOnly.FromDateTime(startDate.Value);
                        if (t.TaskStartDate.HasValue && t.TaskStartDate.Value < start)
                            match = false;
                    }

                    if (endDate.HasValue)
                    {
                        var end = DateOnly.FromDateTime(endDate.Value);
                        if (t.TaskEndDate.HasValue && t.TaskEndDate.Value > end)
                            match = false;
                    }

                    // 🔥 STATUS FILTER
                    if (!string.IsNullOrEmpty(status) && t.TaskStatus != status)
                        match = false;

                    if (match)
                    {
                        filteredTasks.Add(t);

                        var perms = await _permissionService.GetPermissions(employeeId, projId);
                        taskPermissions[t.TaskId] = perms;
                    }
                }
            }

            // EXISTING FILTERS (UNCHANGED)
            if (projectId.HasValue)
                filteredTasks = filteredTasks.Where(t => t.Module.ProjectId == projectId.Value).ToList();

            if (moduleId.HasValue)
                filteredTasks = filteredTasks.Where(t => t.ModuleId == moduleId.Value).ToList();

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

            // DROPDOWNS (UNCHANGED)
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

            // 🔥 KEEP FILTER VALUES
            ViewBag.SelectedProjectId = projectId;
            ViewBag.SelectedModuleId = moduleId;
            ViewBag.Search = search;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(filteredTasks);
        }

        // =========================================
        // 🔥 NEW: EXPORT TO EXCEL
        // =========================================
        public async Task<IActionResult> ExportToExcel(
            int? projectId,
            int? moduleId,
            string search,
            DateTime? startDate,
            DateTime? endDate,
            string status)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var result = await Index(projectId, moduleId, search, startDate, endDate, status) as ViewResult;
            var tasks = result.Model as List<TaskModel>;

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Tasks");

                ws.Cells[1, 1].Value = "Task Name";
                ws.Cells[1, 2].Value = "Module";
                ws.Cells[1, 3].Value = "Status";
                ws.Cells[1, 4].Value = "Start Date";
                ws.Cells[1, 5].Value = "End Date";

                int row = 2;

                foreach (var t in tasks)
                {
                    ws.Cells[row, 1].Value = t.TaskName;
                    ws.Cells[row, 2].Value = t.Module?.ModuleName;
                    ws.Cells[row, 3].Value = t.TaskStatus;
                    ws.Cells[row, 4].Value = t.TaskStartDate?.ToString("yyyy-MM-dd");
                    ws.Cells[row, 5].Value = t.TaskEndDate?.ToString("yyyy-MM-dd");
                    row++;
                }

                return File(package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Tasks.xlsx");
            }
        }
    }
}