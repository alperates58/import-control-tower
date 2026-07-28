namespace ImportControlTower.Application.Models;

public record LoginRequest(string UsernameOrEmail, string Password);

public record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserDto User
);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLoginUtc,
    DateTime CreatedAtUtc,
    List<string> Roles,
    List<string> Permissions
);

public record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    List<string> Roles
);

public record UpdateUserRequest(
    string FullName,
    bool IsActive,
    List<string> Roles
);

public record ResetPasswordResponseDto(
    string TemporaryPassword,
    string Message
);

public record RoleDto(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    List<string> Permissions
);

public record CreateRoleRequest(
    string Name,
    string Description,
    List<string> Permissions
);

public record UpdateRoleRequest(
    string Description,
    List<string> Permissions
);

public record SystemSettingDto(
    string Key,
    string Value,
    string ValueType,
    string Description,
    bool IsSensitive,
    DateTime UpdatedAtUtc,
    Guid? UpdatedByUserId
);

public record UpdateSettingRequest(string Value);

public record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorUsername,
    string ActorType,
    string Action,
    string EntityType,
    string EntityId,
    DateTime TimestampUtc,
    string IpAddress,
    string UserAgent,
    string CorrelationId,
    string MetadataJson
);
