namespace ImportControlTower.Domain.Entities;

public class SystemMigrationHistory
{
    public int Id { get; set; }
    public string MigrationName { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Success";
}
