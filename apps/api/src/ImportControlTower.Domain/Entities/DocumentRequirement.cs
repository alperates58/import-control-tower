using System;

namespace ImportControlTower.Domain.Entities;

public class DocumentRequirement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ScopeType { get; set; } = string.Empty;
    public string? TransportMode { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
}
