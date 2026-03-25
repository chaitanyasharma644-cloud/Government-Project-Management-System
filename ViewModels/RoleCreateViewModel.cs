using System.Collections.Generic;

namespace GPMS.ViewModels
{
    public class RoleCreateViewModel
    {
        // Role Name
        public string RoleName { get; set; } = string.Empty;

        // Role Description (maps to RoleDescription in DB)
        public string? Description { get; set; }

        // List of permissions for checkbox binding
        public List<PermissionCheckbox> Permissions { get; set; } = new List<PermissionCheckbox>();
    }

    public class PermissionCheckbox
    {
        // Will store PermsId
        public int Id { get; set; }

        // Will store PermsName
        public string PermissionName { get; set; } = string.Empty;

        // Checkbox state
        public bool IsSelected { get; set; }
    }
}