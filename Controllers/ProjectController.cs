using GPMS.Data;
using GPMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly AppDbContext _context;

        public ProjectController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        // GET: Project (Project List)
        // =========================================
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                                         .Include(p => p.Modules)
                                         .ToListAsync();

            return View(projects);
        }

        // =========================================
        // GET: Project/Details/
        // =========================================
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Modules)
                    .ThenInclude(m => m.Tasks)
                .Include(p => p.Assignments)
                    .ThenInclude(a => a.Employee)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        // =========================================
        // GET: Project/Create
        // =========================================
        public IActionResult Create()
        {
            return View();
        }

        // =========================================
        // POST: Project/Create
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (ModelState.IsValid)
            {
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // =========================================
        // GET: Project/Edit/
        // =========================================
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // =========================================
        // POST: Project/Edit/
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.ProjectId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Projects.Any(e => e.ProjectId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // =========================================
        // POST: Project/Delete/
        // =========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}