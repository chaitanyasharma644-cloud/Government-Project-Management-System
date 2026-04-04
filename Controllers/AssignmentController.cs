using GPMS.Data;
using GPMS.Models;
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
            // type = "project" / "module" / "task"

            ViewBag.EmployeeList = new SelectList(
                _context.Employees,
                "EmployeeId",
                "EmployeeName"
            );

            ViewBag.Type = type;
            ViewBag.RefId = id;

            return View();
        }

        // =========================================
        // 🔹 CREATE ASSIGNMENT (POST)
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEmployee(int employeeId, int refId, string type)
        {
            // 🚫 Prevent duplicate assignment
            bool exists = await _context.Assignments.AnyAsync(a =>
                a.EmployeeId == employeeId &&
                (
                    (type == "project" && a.ProjectId == refId) ||
                    (type == "module" && a.ModuleId == refId) ||
                    (type == "task" && a.TaskId == refId)
                )
            );

            if (exists)
            {
                return Content("Employee already assigned!");
            }

            var assignment = new Assignment
            {
                EmployeeId = employeeId,
                AssignedDate = DateOnly.FromDateTime(DateTime.Now)
            };

            // 🔥 Assign correct FK
            if (type == "project")
                assignment.ProjectId = refId;

            else if (type == "module")
                assignment.ModuleId = refId;

            else if (type == "task")
                assignment.TaskId = refId;

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // 🔁 Redirect back
            return type switch
            {
                "project" => RedirectToAction("Details", "Project", new { id = refId }),
                "module" => RedirectToAction("Details", "Module", new { id = refId }),
                "task" => RedirectToAction("Details", "Task", new { id = refId }),
                _ => RedirectToAction("Index", "Project")
            };
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

            // 🔁 Redirect based on level
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

            // 🔁 Redirect back
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
