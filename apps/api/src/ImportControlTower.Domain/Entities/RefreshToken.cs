namespace ImportControlTower.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedByIp { get; set; } = string.Empty;
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    // Concurrency token for PostgreSQL
    public uint Xmin { get; set; }
}
