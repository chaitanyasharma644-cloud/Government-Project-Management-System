using GPMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

// ✅ ALIAS (IMPORTANT FIX)
using TaskModel = GPMS.Models.Task;

namespace GPMS.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly AppDbContext _context;

        public TaskController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // ✅ TASK LIST WITH FILTERING
        // ==============================
        public async System.Threading.Tasks.Task<IActionResult> Index(int? projectId, int? moduleId, string search)
        {
            var tasks = _context.Tasks
                .Include(t => t.Module)
                .ThenInclude(m => m.Project)
                .AsQueryable();

            // 🔍 Filter by Project
            if (projectId.HasValue)
            {
                tasks = tasks.Where(t => t.Module.ProjectId == projectId);
            }

            // 🔍 Filter by Module
            if (moduleId.HasValue)
            {
                tasks = tasks.Where(t => t.ModuleId == moduleId);
            }

            // 🔍 Search by Task Name
            if (!string.IsNullOrEmpty(search))
            {
                tasks = tasks.Where(t => t.TaskName.Contains(search));
            }

            // ✅ Dropdowns (keep selected values)
            ViewBag.Projects = new SelectList(
                _context.Projects,
                "ProjectId",
                "ProjectName",
                projectId
            );

            ViewBag.Modules = new SelectList(
                _context.Modules,
                "ModuleId",
                "ModuleName",
                moduleId
            );

            return View(await tasks.ToListAsync());
        }

        // ==============================
        // TASK DETAILS
        // ==============================
        public async System.Threading.Tasks.Task<IActionResult> Details(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Module)
                .ThenInclude(m => m.Project)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        // ==============================
        // CREATE TASK (GET)
        // ==============================
        public IActionResult Create()
        {
            ViewBag.ProjectList = new SelectList(
                _context.Projects,
                "ProjectId",
                "ProjectName"
            );

            ViewBag.ModuleList = new SelectList(
                _context.Modules,
                "ModuleId",
                "ModuleName"
            );

            return View();
        }

        // ==============================
        // CREATE TASK (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskModel task)
        {
            // 1. Check if the model is actually coming in
            if (task == null) return Content("Task object is null. Binding failed.");

            // 2. Identify EXACTLY which field is causing the failure
            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors)
                                       .Select(x => x.ErrorMessage).ToList();

                // This will stop the app and show you the errors in the browser
                return Content("Validation Failed: " + string.Join(" | ", errors));
            }

            try
            {
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Capture the InnerException (this is where DB errors like Foreign Keys hide)
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                return Content($"Database Error: {ex.Message} | Inner: {innerMsg}");
            }
        }

        // ==============================
        // EDIT TASK (GET)
        // ==============================
        public async System.Threading.Tasks.Task<IActionResult> Edit(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Module)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
                return NotFound();

            task.ProjectId = task.Module?.ProjectId;

            ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName", task.ProjectId);
            ViewBag.ModuleList = new SelectList(_context.Modules, "ModuleId", "ModuleName", task.ModuleId);

            return View(task);
        }

        // ==============================
        // EDIT TASK (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> Edit(TaskModel task)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProjectList = new SelectList(_context.Projects, "ProjectId", "ProjectName", task.ProjectId);
                ViewBag.ModuleList = new SelectList(_context.Modules, "ModuleId", "ModuleName", task.ModuleId);

                return View(task);
            }

            var existingTask = await _context.Tasks.FindAsync(task.TaskId);

            if (existingTask == null)
                return NotFound();

            existingTask.ModuleId = task.ModuleId;
            existingTask.TaskName = task.TaskName;
            existingTask.TaskDescription = task.TaskDescription;
            existingTask.TaskStatus = task.TaskStatus;
            existingTask.TaskStartDate = task.TaskStartDate;
            existingTask.TaskEndDate = task.TaskEndDate;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // DELETE TASK
        // ==============================
        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // AJAX: GET MODULES BY PROJECT
        // ==============================
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