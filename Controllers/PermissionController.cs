using GPMS.Data;
using GPMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Controllers
{
    [Authorize]
    public class PermissionController : Controller
    {
        private readonly AppDbContext _context;

        public PermissionController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ INDEX (List all permissions)
        public async Task<IActionResult> Index(string search)
        {
            var query = _context.Permissions.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.PermsName.Contains(search));
            }

            var permissions = await query.ToListAsync();
            return View(permissions);
        }

        // ✅ CREATE (GET)
        public IActionResult Create()
        {
            return View();
        }

        // ✅ CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Permission permission)
        {
            if (ModelState.IsValid)
            {
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(permission);
        }
    }
}