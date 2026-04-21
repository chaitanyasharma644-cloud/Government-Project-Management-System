using GPMS.Data;
using GPMS.Models;
using GPMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml; 
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
        // 🔥 UPDATED: Index WITH FILTERS
        // =========================================
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string status)
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
            var modulePermissions = new Dictionary<int, List<string>>();

            foreach (var m in allModules)
            {
                bool isAssigned = await _context.Assignments
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == m.ProjectId);

                bool canView = await _permissionService.HasPermission(employeeId, m.ProjectId, "ViewModule");

                if (employee.IsAdmin || (isAssigned && canView))
                {
                    bool match = true;

                    // ✅ START DATE
                    if (startDate.HasValue)
                    {
                        var start = DateOnly.FromDateTime(startDate.Value);
                        if (m.ModuleStartDate.HasValue && m.ModuleStartDate.Value < start)
                            match = false;
                    }

                    // ✅ END DATE
                    if (endDate.HasValue)
                    {
                        var end = DateOnly.FromDateTime(endDate.Value);
                        if (m.ModuleEndDate.HasValue && m.ModuleEndDate.Value > end)
                            match = false;
                    }

                    // ✅ STATUS
                    if (!string.IsNullOrEmpty(status) && m.ModuleStatus != status)
                        match = false;

                    if (match)
                    {
                        filteredModules.Add(m);

                        var perms = await _permissionService.GetPermissions(employeeId, m.ProjectId);
                        modulePermissions[m.ModuleId] = perms;
                    }
                }
            }

            ViewBag.ModulePermissions = modulePermissions;
            ViewBag.CanCreateModule = await _permissionService.HasPermission(employeeId, null, "CreateModule");
            ViewBag.Projects = await _context.Projects.ToListAsync();

            return View(filteredModules);
        }

        // =========================================
        // 🔥 NEW: ExportToExcel
        // =========================================
        public async Task<IActionResult> ExportToExcel(DateTime? startDate, DateTime? endDate, string status)
        {
            // 🔥 LICENSE FIX
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

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

            foreach (var m in allModules)
            {
                bool isAssigned = await _context.Assignments
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ProjectId == m.ProjectId);

                bool canView = await _permissionService.HasPermission(employeeId, m.ProjectId, "ViewModule");

                if (employee.IsAdmin || (isAssigned && canView))
                {
                    bool match = true;

                    if (startDate.HasValue)
                    {
                        var start = DateOnly.FromDateTime(startDate.Value);
                        if (m.ModuleStartDate.HasValue && m.ModuleStartDate.Value < start)
                            match = false;
                    }

                    if (endDate.HasValue)
                    {
                        var end = DateOnly.FromDateTime(endDate.Value);
                        if (m.ModuleEndDate.HasValue && m.ModuleEndDate.Value > end)
                            match = false;
                    }

                    if (!string.IsNullOrEmpty(status) && m.ModuleStatus != status)
                        match = false;

                    if (match)
                        filteredModules.Add(m);
                }
            }

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Modules");

                ws.Cells[1, 1].Value = "Module Name";
                ws.Cells[1, 2].Value = "Project";
                ws.Cells[1, 3].Value = "Tasks Count";
                ws.Cells[1, 4].Value = "Start Date";
                ws.Cells[1, 5].Value = "End Date";
                ws.Cells[1, 6].Value = "Status";

                int row = 2;

                foreach (var m in filteredModules)
                {
                    ws.Cells[row, 1].Value = m.ModuleName;
                    ws.Cells[row, 2].Value = m.Project?.ProjectName;
                    ws.Cells[row, 3].Value = m.Tasks.Count;
                    ws.Cells[row, 4].Value = m.ModuleStartDate?.ToString("yyyy-MM-dd");
                    ws.Cells[row, 5].Value = m.ModuleEndDate?.ToString("yyyy-MM-dd");
                    ws.Cells[row, 6].Value = m.ModuleStatus;
                    row++;
                }

                return File(package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Modules.xlsx");
            }
        }
    }
}