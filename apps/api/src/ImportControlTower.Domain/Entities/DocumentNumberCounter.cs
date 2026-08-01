using System;

namespace ImportControlTower.Domain.Entities;

public class DocumentNumberCounter
{
    public string DocumentType { get; set; } = string.Empty;
    public int Year { get; set; }
    public long LastNumber { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
