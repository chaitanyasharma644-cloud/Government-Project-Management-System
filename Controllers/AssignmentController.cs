using GPMS.Data;
using GPMS.Models;
using GPMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly AppDbContext _context;

        public AssignmentController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // 🔹 CREATE ASSIGNMENT (GET)
        // =========================================
        public IActionResult AssignEmployee(int id, string type)
        {
            var model = new AssignmentViewModel
            {
                Employees = new SelectList(
                    _context.Employees.ToList(),   // 🔥 MUST BE ToList()
                    "EmployeeId",
                    "EmployeeName"
                ),

                Projects = new SelectList(_context.Projects, "ProjectId", "ProjectName"),
                Modules = new SelectList(_context.Modules, "ModuleId", "ModuleName"),
                Tasks = new SelectList(_context.Tasks, "TaskId", "TaskName"),
                Roles = new SelectList(_context.Roles, "RoleId", "RoleName"),

                CurrentLevel = type
            };

            return View(model);
        }

        // =========================================
        // 🔹 CREATE ASSIGNMENT (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEmployee(AssignmentViewModel model)
        {
            // 🚫 Check employee selected
            if (model.EmployeeId == 0)
            {
                ModelState.AddModelError("EmployeeId", "Please select an employee.");
            }

            // 🚫 Check if employee exists in DB
            bool employeeExists = await _context.Employees
                .AnyAsync(e => e.EmployeeId == model.EmployeeId);

            if (!employeeExists)
            {
                ModelState.AddModelError("", "Invalid Employee selected.");
            }

            if (!ModelState.IsValid)
            {
                // reload dropdowns
                model.Employees = new SelectList(_context.Employees, "EmployeeId", "EmployeeName");
                model.Projects = new SelectList(_context.Projects, "ProjectId", "ProjectName");
                model.Modules = new SelectList(_context.Modules, "ModuleId", "ModuleName");
                model.Tasks = new SelectList(_context.Tasks, "TaskId", "TaskName");
                model.Roles = new SelectList(_context.Roles, "RoleId", "RoleName");

                return View(model);
            }

            var assignment = new Assignment
            {
                EmployeeId = model.EmployeeId,
                RoleId = model.RoleId,
                AssignedDate = model.AssignedDate
            };

            if (model.ProjectId != null)
                assignment.ProjectId = model.ProjectId;

            if (model.ModuleId != null)
                assignment.ModuleId = model.ModuleId;

            if (model.TaskId != null)
                assignment.TaskId = model.TaskId;

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Project");
        }

        // =========================================
        // 🔹 EDIT ASSIGNMENT (GET)
        // =========================================
        public async Task<IActionResult> EditAssignment(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.AssignmentId == id);

            if (assignment == null)
                return NotFound();

            ViewBag.EmployeeList = new SelectList(
                _context.Employees,
                "EmployeeId",
                "EmployeeName",
                assignment.EmployeeId
            );

            return View(assignment);
        }

        // =========================================
        // 🔹 EDIT ASSIGNMENT (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssignment(Assignment model)
        {
            var assignment = await _context.Assignments.FindAsync(model.AssignmentId);

            if (assignment == null)
                return NotFound();

            assignment.EmployeeId = model.EmployeeId;

            await _context.SaveChangesAsync();

            if (assignment.ProjectId != null)
                return RedirectToAction("Details", "Project", new { id = assignment.ProjectId });

            if (assignment.ModuleId != null)
                return RedirectToAction("Details", "Module", new { id = assignment.ModuleId });

            if (assignment.TaskId != null)
                return RedirectToAction("Details", "Task", new { id = assignment.TaskId });

            return RedirectToAction("Index", "Project");
        }

        // =========================================
        // 🔹 DELETE ASSIGNMENT
        // =========================================
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);

            if (assignment == null)
                return NotFound();

            int? projectId = assignment.ProjectId;
            int? moduleId = assignment.ModuleId;
            int? taskId = assignment.TaskId;

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            if (projectId != null)
                return RedirectToAction("Details", "Project", new { id = projectId });

            if (moduleId != null)
                return RedirectToAction("Details", "Module", new { id = moduleId });

            if (taskId != null)
                return RedirectToAction("Details", "Task", new { id = taskId });

            return RedirectToAction("Index", "Project");
        }
    }
}