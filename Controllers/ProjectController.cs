using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; 
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

        private int GetEmployeeId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim == null)
                throw new Exception("User not logged in properly.");

            return int.Parse(claim.Value);
        }

        // =========================================
        // 🔥 UPDATED: Index with Filters
        // =========================================
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string status)
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
                    bool match = true;

                    // ✅ START DATE FILTER
                    if (startDate.HasValue)
                    {
                        DateOnly start = DateOnly.FromDateTime(startDate.Value);

                        if (p.ProjectStartDate < start)
                            match = false;
                    }

                    // ✅ END DATE FILTER
                    if (endDate.HasValue)
                    {
                        DateOnly end = DateOnly.FromDateTime(endDate.Value);

                        if (p.ProjectEndDate.HasValue && p.ProjectEndDate.Value > end)
                            match = false;
                    }

                    // 🔥 STATUS FILTER
                    if (!string.IsNullOrEmpty(status) && p.ProjectStatus != status)
                        match = false;

                    if (match)
                    {
                        filteredProjects.Add(p);

                        var perms = await _permissionService.GetPermissions(employeeId, p.ProjectId);
                        projectPermissions[p.ProjectId] = perms;
                    }
                }
            }

            ViewBag.CanCreate = await _permissionService.HasPermission(employeeId, null, "CreateProject");
            ViewBag.ProjectPermissions = projectPermissions;

            return View(filteredProjects);
        }

        // =========================================
        // 🔥 NEW: Export to Excel
        // =========================================

        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate, string status)
            {
            // 🔥 REQUIRED FIX (THIS WAS CAUSING YOUR ERROR)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var employeeId = GetEmployeeId();

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                    return RedirectToAction("Login", "Account");

                var allProjects = await _context.Projects
                    .Include(p => p.Modules)
                    .ToListAsync();

                var filteredProjects = new List<Project>();

                foreach (var p in allProjects)
                {
                    bool isAssigned = await _context.Assignments
                        .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == p.ProjectId);

                    bool canView = await _permissionService.HasPermission(employeeId, p.ProjectId, "ViewProject");

                    if (employee.IsAdmin || (isAssigned && canView))
                    {
                        bool match = true;

                        // ✅ START DATE FILTER
                        if (startDate.HasValue)
                        {
                            DateOnly start = DateOnly.FromDateTime(startDate.Value);

                            if (p.ProjectStartDate < start)
                                match = false;
                        }

                        // ✅ END DATE FILTER
                        if (endDate.HasValue)
                        {
                            DateOnly end = DateOnly.FromDateTime(endDate.Value);

                            if (p.ProjectEndDate.HasValue && p.ProjectEndDate.Value > end)
                                match = false;
                        }

                        // ✅ STATUS FILTER
                        if (!string.IsNullOrEmpty(status) && p.ProjectStatus != status)
                            match = false;

                        if (match)
                            filteredProjects.Add(p);
                    }
                }

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Projects");

                    // 🔥 HEADERS
                    ws.Cells[1, 1].Value = "Project Name";
                    ws.Cells[1, 2].Value = "Modules Count";
                    ws.Cells[1, 3].Value = "Start Date";
                    ws.Cells[1, 4].Value = "End Date";
                    ws.Cells[1, 5].Value = "Status";

                    int row = 2;

                    foreach (var p in filteredProjects)
                    {
                        ws.Cells[row, 1].Value = p.ProjectName;
                        ws.Cells[row, 2].Value = p.Modules.Count;
                        ws.Cells[row, 3].Value = p.ProjectStartDate.ToString("yyyy-MM-dd");
                        ws.Cells[row, 4].Value = p.ProjectEndDate?.ToString("yyyy-MM-dd");
                        ws.Cells[row, 5].Value = p.ProjectStatus;
                        row++;
                    }

                    return File(package.GetAsByteArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Projects.xlsx");
                }
            }

    // =========================================
    // (REST OF YOUR CODE UNCHANGED)
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

            bool isAssigned = await _context.Assignments
                .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == id);

            bool canView = await _permissionService.HasPermission(employeeId, id, "ViewProject");

            if (!employee.IsAdmin && (!isAssigned || !canView))
                return Forbid();

            ViewBag.CanEditProject = await _permissionService.HasPermission(employeeId, id, "EditProject");
            ViewBag.CanDeleteProject = await _permissionService.HasPermission(employeeId, id, "DeleteProject");
            ViewBag.CanCreateModule = await _permissionService.HasPermission(employeeId, id, "CreateModule");

            var modulePermissions = new Dictionary<int, List<string>>();

            foreach (var m in project.Modules)
            {
                var perms = await _permissionService.GetPermissions(employeeId, id);
                modulePermissions[m.ModuleId] = perms;
            }

            ViewBag.ModulePermissions = modulePermissions;

            ViewBag.CanViewEmployee = await _permissionService.HasPermission(employeeId, id, "ViewAssignment");
            ViewBag.CanEditEmployee = await _permissionService.HasPermission(employeeId, id, "EditAssignment");

            return View(project);
        }

        public async Task<IActionResult> Create()
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, null, "CreateProject"))
                return Forbid();

            return View();
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employeeId = GetEmployeeId();

            if (!await _permissionService.HasPermission(employeeId, id, "DeleteProject"))
                return Forbid();

            var project = await _context.Projects
                .Include(p => p.Modules)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
                return NotFound();

            if (project.Modules.Any())
            {
                int moduleCount = project.Modules.Count;

                TempData["Error"] = $"Cannot delete project. It has {moduleCount} modules. Delete modules first.";
                return RedirectToAction("Details", new { id });
            }

            bool hasAssignments = await _context.Assignments
                .AnyAsync(a => a.ProjectId == id
                    || a.Module.ProjectId == id);

            if (hasAssignments)
            {
                int assignmentCount = await _context.Assignments
                    .CountAsync(a => a.ProjectId == id
                        || a.Module.ProjectId == id);

                TempData["Error"] = $"Cannot delete project. It has {assignmentCount} assignments. Remove them first.";
                return RedirectToAction("Details", new { id });
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Project deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}