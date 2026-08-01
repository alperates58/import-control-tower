using System;

namespace ImportControlTower.Domain.Entities;

public class DocumentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public int VersionNumber { get; set; } = 1;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;

    public string StorageStatus { get; set; } = "Pending";
    public bool IsCurrent { get; set; } = false;
    public string Status { get; set; } = "Active";

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid UploadedByUserId { get; set; }
    public ApplicationUser? UploadedByUser { get; set; }
}
