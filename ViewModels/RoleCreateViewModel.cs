using System.Collections.Generic;

namespace GPMS.ViewModels
{
    public class RoleCreateViewModel
    {
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<PermissionCheckbox> Permissions { get; set; } = new List<PermissionCheckbox>();
    }

    public class PermissionCheckbox
    {
        public int Id { get; set; }

        public string PermissionName { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }
}