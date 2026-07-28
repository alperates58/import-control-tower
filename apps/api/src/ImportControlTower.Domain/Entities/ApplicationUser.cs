using Microsoft.AspNetCore.Identity;

namespace ImportControlTower.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public int AuthVersion { get; set; } = 1;
    public DateTime? LastLoginUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
