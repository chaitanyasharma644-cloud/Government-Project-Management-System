using GPMS.Data;
using GPMS.Models;
using GPMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Controllers
{
    [Authorize]
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // ROLE LIST + SEARCH
        // ==============================
        public async Task<IActionResult> Index(string search)
        {
            var roles = _context.Roles
                .Include(r => r.RolePermissions)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                roles = roles.Where(r => r.RoleName.Contains(search));
            }

            return View(await roles.ToListAsync());
        }

        // ==============================
        // ROLE DETAILS
        // ==============================
        public async Task<IActionResult> Details(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
                return NotFound();

            return View(role);
        }

        // ==============================
        // CREATE ROLE (GET)
        // ==============================
        public async Task<IActionResult> Create()
        {
            var permissions = await _context.Permissions.ToListAsync();

            var model = new RoleCreateViewModel
            {
                Permissions = permissions.Select(p => new PermissionCheckbox
                {
                    Id = p.PermsId,
                    PermissionName = p.PermsName,
                    IsSelected = false
                }).ToList()
            };

            return View(model);
        }

        // ==============================
        // CREATE ROLE (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload permissions if validation fails
                var permissions = await _context.Permissions.ToListAsync();

                model.Permissions = permissions.Select(p => new PermissionCheckbox
                {
                    Id = p.PermsId,
                    PermissionName = p.PermsName,
                    IsSelected = false
                }).ToList();

                return View(model);
            }

            var role = new Role
            {
                RoleName = model.RoleName,
                RoleDescription = model.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add selected permissions
            var selectedPermissions = model.Permissions
                .Where(p => p.IsSelected)
                .Select(p => new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = p.Id
                });

            _context.RolePermissions.AddRange(selectedPermissions);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // EDIT ROLE (GET) ✅ FIXED
        // ==============================
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
                return NotFound();

            // ✅ STEP 1: Get selected IDs (IN MEMORY)
            var selectedIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToList();

            // ✅ STEP 2: Get all permissions
            var allPermissions = await _context.Permissions.ToListAsync();

            // ✅ STEP 3: Build ViewModel safely
            var model = new RoleCreateViewModel
            {
                RoleName = role.RoleName,
                Description = role.RoleDescription,

                Permissions = allPermissions.Select(p => new PermissionCheckbox
                {
                    Id = p.PermsId,
                    PermissionName = p.PermsName,
                    IsSelected = selectedIds.Contains(p.PermsId)
                }).ToList()
            };

            return View(model);
        }

        // ==============================
        // EDIT ROLE (POST)
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoleCreateViewModel model)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                // Reload permissions on error
                var permissions = await _context.Permissions.ToListAsync();

                model.Permissions = permissions.Select(p => new PermissionCheckbox
                {
                    Id = p.PermsId,
                    PermissionName = p.PermsName,
                    IsSelected = false
                }).ToList();

                return View(model);
            }

            // Update role
            role.RoleName = model.RoleName;
            role.RoleDescription = model.Description;

            await _context.SaveChangesAsync();

            // Remove old permissions
            var existingPermissions = _context.RolePermissions
                .Where(rp => rp.RoleId == id);

            _context.RolePermissions.RemoveRange(existingPermissions);
            await _context.SaveChangesAsync();

            // Add new permissions
            var newPermissions = model.Permissions
                .Where(p => p.IsSelected)
                .Select(p => new RolePermission
                {
                    RoleId = id,
                    PermissionId = p.Id
                });

            _context.RolePermissions.AddRange(newPermissions);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==============================
        // DELETE ROLE
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role != null)
            {
                var rolePermissions = _context.RolePermissions
                    .Where(rp => rp.RoleId == id);

                _context.RolePermissions.RemoveRange(rolePermissions);
                _context.Roles.Remove(role);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}