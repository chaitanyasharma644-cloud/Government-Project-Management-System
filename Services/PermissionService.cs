using GPMS.Data;
using Microsoft.EntityFrameworkCore;

namespace GPMS.Services
{
    public class PermissionService
    {
        private readonly AppDbContext _context;

        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        // =========================================
        //  ADMIN CHECK
        // =========================================
        private async Task<bool> IsAdmin(int employeeId)
        {
            return await _context.Employees
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => e.IsAdmin)
                .FirstOrDefaultAsync();
        }

        // =========================================
        //  GET ROLE IDS
        // =========================================
        private async Task<List<int>> GetRoleIds(int employeeId, int? projectId)
        {
            var query = _context.Assignments
                .Where(a => a.EmployeeId == employeeId);

            //  If projectId provided → filter by project
            if (projectId.HasValue)
            {
                query = query.Where(a => a.ProjectId == projectId.Value);
            }

            return await query
                .Select(a => a.RoleId)
                .ToListAsync();
        }

        // =========================================
        //  CHECK SINGLE PERMISSION
        // =========================================
        public async Task<bool> HasPermission(int employeeId, int? projectId, string permissionName)
        {
            //  ADMIN BYPASS
            if (await IsAdmin(employeeId))
                return true;

            var roleIds = await GetRoleIds(employeeId, projectId);

            if (!roleIds.Any())
                return false;

            return await _context.RolePermissions
                .Include(rp => rp.Permission)
                .AnyAsync(rp =>
                    roleIds.Contains(rp.RoleId) &&
                    rp.Permission.PermsName == permissionName
                );
        }

        // =========================================
        //  GET ALL PERMISSIONS
        // =========================================
        public async Task<List<string>> GetPermissions(int employeeId, int? projectId)
        {
            //  ADMIN → ALL PERMISSIONS
            if (await IsAdmin(employeeId))
            {
                return await _context.Permissions
                    .Select(p => p.PermsName)
                    .ToListAsync();
            }

            var roleIds = await GetRoleIds(employeeId, projectId);

            if (!roleIds.Any())
                return new List<string>();

            return await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.PermsName)
                .Distinct()
                .ToListAsync();
        }

        // =========================================
        //  CHECK ANY PERMISSION
        // =========================================
        public async Task<bool> HasAnyPermission(int employeeId, int? projectId, params string[] permissions)
        {
            if (await IsAdmin(employeeId))
                return true;

            var roleIds = await GetRoleIds(employeeId, projectId);

            if (!roleIds.Any())
                return false;

            return await _context.RolePermissions
                .AnyAsync(rp =>
                    roleIds.Contains(rp.RoleId) &&
                    permissions.Contains(rp.Permission.PermsName)
                );
        }

        // =========================================
        //  CHECK ALL PERMISSIONS
        // =========================================
        public async Task<bool> HasAllPermissions(int employeeId, int? projectId, params string[] permissions)
        {
            if (await IsAdmin(employeeId))
                return true;

            var userPermissions = await GetPermissions(employeeId, projectId);

            return permissions.All(p => userPermissions.Contains(p));
        }
    }
}