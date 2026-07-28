using Microsoft.AspNetCore.Identity;

namespace ImportControlTower.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; } = false;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
