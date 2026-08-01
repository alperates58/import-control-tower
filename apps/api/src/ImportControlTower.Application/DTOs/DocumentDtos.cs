using System;
using System.Collections.Generic;

namespace ImportControlTower.Application.DTOs;

public class CreateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }

    public Guid? ImportCaseId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? ShipmentContainerId { get; set; }
}

public class UpdateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid? ImportCaseId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? ShipmentContainerId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public uint RowVersion { get; set; }

    public DocumentVersionDto? CurrentVersion { get; set; }
}

public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredObjectKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public string StorageStatus { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string? UploadedByUserName { get; set; }
}

public class DocumentChecklistDto
{
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public int TotalRequiredCount { get; set; }
    public int CompletedCount { get; set; }
    public int MissingCount { get; set; }
    public string Status { get; set; } = "Complete"; // Complete, Missing, Expired
    public List<DocumentChecklistItemDto> Items { get; set; } = new();
}

public class DocumentChecklistItemDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string Status { get; set; } = "Missing"; // Complete, Missing, Expired, ExpiringSoon
    public Guid? LinkedDocumentId { get; set; }
    public string? DocumentTitle { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
