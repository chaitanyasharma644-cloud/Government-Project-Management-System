using GPMS.Data;
using GPMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Controllers
{
    [Authorize]
    public class ModuleController : Controller
    {
        private readonly AppDbContext _context;

        public ModuleController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // MODULE LIST
        // ==============================
        public async Task<IActionResult> Index(int? projectId)
        {
            ViewBag.Projects = await _context.Projects.ToListAsync();

            var modulesQuery = _context.Modules
                .Include(m => m.Tasks)
                .Include(m => m.Project)
                .AsQueryable();

            if (projectId != null)
            {
                modulesQuery = modulesQuery.Where(m => m.ProjectId == projectId);
            }

            return View(await modulesQuery.ToListAsync());
        }

        // ==============================
        // MODULE DETAILS (NEW ✅)
        // ==============================
        public async Task<IActionResult> Details(int id)
        {
            var module = await _context.Modules
                .Include(m => m.Project)
                .Include(m => m.Tasks)

                // 🔥 ADD THIS
                .Include(m => m.Assignments)
                    .ThenInclude(a => a.Employee)

                .FirstOrDefaultAsync(m => m.ModuleId == id);

            if (module == null)
            {
                return NotFound();
            }

            return View(module);
        }

        // ==============================
        // EDIT MODULE (GET)
        // ==============================
        public async Task<IActionResult> Edit(int id)
        {
            var module = await _context.Modules.FindAsync(id);

            if (module == null)
                return NotFound();

            ViewBag.Projects = new SelectList(
                _context.Projects,
                "ProjectId",
                "ProjectName",
                module.ProjectId
            );

            return View(module);
        }

        // ==============================
        // EDIT MODULE (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Module module)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Projects = new SelectList(
                    _context.Projects,
                    "ProjectId",
                    "ProjectName",
                    module.ProjectId
                );

                return View(module);
            }

            try
            {
                // 🔥 safer update
                var existingModule = await _context.Modules.FindAsync(module.ModuleId);

                if (existingModule == null)
                    return NotFound();

                existingModule.ModuleName = module.ModuleName;
                existingModule.ProjectId = module.ProjectId;
                existingModule.Details = module.Details;
                existingModule.ModuleStatus = module.ModuleStatus;
                existingModule.ModuleStartDate = module.ModuleStartDate;
                existingModule.ModuleEndDate = module.ModuleEndDate;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // DELETE MODULE
        // ==============================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var module = await _context.Modules.FindAsync(id);

            if (module != null)
            {
                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ======================
        // GET: Module/Create
        // ======================
        public IActionResult Create()
        {
            ViewBag.ProjectList = new SelectList(
                _context.Projects,
                "ProjectId",
                "ProjectName"
            );

            return View();
        }

        // ======================
        // POST: Module/Create
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Module module)
        {
            if (ModelState.IsValid)
            {
                _context.Modules.Add(module);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.ProjectList = new SelectList(
                _context.Projects,
                "ProjectId",
                "ProjectName",
                module.ProjectId
            );

            return View(module);
        }
    }
}