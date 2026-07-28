namespace ImportControlTower.Domain.Entities;

public class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "String"; // String, Boolean, Integer, Json
    public string Description { get; set; } = string.Empty;
    public bool IsSensitive { get; set; } = false;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? UpdatedByUserId { get; set; }
    public virtual ApplicationUser? UpdatedByUser { get; set; }
}
