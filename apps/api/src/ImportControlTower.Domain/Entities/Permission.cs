namespace ImportControlTower.Domain.Entities;

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public virtual ApplicationRole Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public virtual Permission Permission { get; set; } = null!;
}
