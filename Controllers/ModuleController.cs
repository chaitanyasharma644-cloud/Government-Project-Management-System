using GPMS.Data;
using GPMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            // Send projects for dropdown
            ViewBag.Projects = await _context.Projects.ToListAsync();

            var modulesQuery = _context.Modules
                .Include(m => m.Tasks)
                .Include(m => m.Project)
                .AsQueryable();

            // Filter by selected project
            if (projectId != null)
            {
                modulesQuery = modulesQuery.Where(m => m.ProjectId == projectId);
            }

            var modules = await modulesQuery.ToListAsync();

            return View(modules);
        }
    }
}