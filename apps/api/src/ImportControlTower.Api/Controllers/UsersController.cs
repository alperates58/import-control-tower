using System.Security.Claims;
using System.Security.Cryptography;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IMemoryCache _cache;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext,
        IAuditLogService auditLogService,
        IMemoryCache cache)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _cache = cache;
    }

    [HttpGet]
    [Authorize(Policy = "users.view")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => u.Email!.Contains(search) || u.FullName.Contains(search) || u.UserName!.Contains(search));
        }

        var users = await query.OrderByDescending(u => u.CreatedAtUtc).ToListAsync(cancellationToken);
        var result = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.FullName,
                user.IsActive,
                user.MustChangePassword,
                user.LastLoginUtc,
                user.CreatedAtUtc,
                roles.ToList(),
                new List<string>()
            ));
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "users.view")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { detail = "Kullanıcı bulunamadı." });

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = await BuildUserDtoAsync(user, roles.ToList());
        return Ok(userDto);
    }

    [HttpPost]
    [Authorize(Policy = "users.create")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { detail = "Bu e-posta adresiyle kayıtlı kullanıcı mevcut." });
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true,
            IsActive = true,
            MustChangePassword = true,
            AuthVersion = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { detail = string.Join(", ", createResult.Errors.Select(e => e.Description)) });
        }

        if (request.Roles != null && request.Roles.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, request.Roles);
        }

        var actorUserId = GetCurrentUserId();
        await _auditLogService.LogAsync(
            action: "User.Created",
            entityType: "User",
            entityId: user.Id.ToString(),
            actorUserId: actorUserId,
            actorUsername: User.Identity?.Name,
            metadata: new { email = user.Email, roles = request.Roles },
            cancellationToken: cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = await BuildUserDtoAsync(user, roles.ToList());
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userDto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "users.edit")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { detail = "Kullanıcı bulunamadı." });

        var currentRoles = await _userManager.GetRolesAsync(user);

        // Protection: Check if removing SystemAdmin role from last active SystemAdmin
        if (currentRoles.Contains("SystemAdmin") && !request.Roles.Contains("SystemAdmin"))
        {
            var isLastAdmin = await IsLastActiveSystemAdminAsync(user.Id);
            if (isLastAdmin)
            {
                return Conflict(new { detail = "Sistemdeki son aktif SystemAdmin rolü bu kullanıcıdan çıkarılamaz." });
            }
        }

        // Protection: Self-disable check
        var currentActorId = GetCurrentUserId();
        if (currentActorId == user.Id && !request.IsActive)
        {
            return Conflict(new { detail = "Kendi hesabınızı pasifleştiremezsiniz." });
        }

        // Protection: Disable last active SystemAdmin
        if (currentRoles.Contains("SystemAdmin") && !request.IsActive)
        {
            var isLastAdmin = await IsLastActiveSystemAdminAsync(user.Id);
            if (isLastAdmin)
            {
                return Conflict(new { detail = "Sistemdeki son aktif SystemAdmin kullanıcısı pasifleştirilemez." });
            }
        }

        user.FullName = request.FullName;
        user.IsActive = request.IsActive;
        user.AuthVersion += 1; // Increment AuthVersion on role/status change

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return BadRequest(new { detail = string.Join(", ", updateResult.Errors.Select(e => e.Description)) });
        }

        // Update Roles
        var rolesToRemove = currentRoles.Except(request.Roles).ToList();
        var rolesToAdd = request.Roles.Except(currentRoles).ToList();

        if (rolesToRemove.Count > 0) await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        if (rolesToAdd.Count > 0) await _userManager.AddToRolesAsync(user, rolesToAdd);

        // Clear Caches
        _cache.Remove($"UserAuthStatus:{user.Id}");
        _cache.Remove($"UserPermissions:{user.Id}");

        // Revoke active refresh tokens
        var activeTokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == user.Id && !rt.IsRevoked).ToListAsync(cancellationToken);
        foreach (var t in activeTokens) { t.IsRevoked = true; t.RevokedAtUtc = DateTime.UtcNow; }
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            action: "User.Updated",
            entityType: "User",
            entityId: user.Id.ToString(),
            actorUserId: currentActorId,
            actorUsername: User.Identity?.Name,
            metadata: new { fullName = user.FullName, isActive = user.IsActive, roles = request.Roles },
            cancellationToken: cancellationToken);

        var updatedRoles = await _userManager.GetRolesAsync(user);
        var userDto = await BuildUserDtoAsync(user, updatedRoles.ToList());
        return Ok(userDto);
    }

    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = "users.disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DisableUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var currentActorId = GetCurrentUserId();
        if (currentActorId == user.Id)
        {
            return Conflict(new { detail = "Kendi hesabınızı pasifleştiremezsiniz." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("SystemAdmin") && await IsLastActiveSystemAdminAsync(user.Id))
        {
            return Conflict(new { detail = "Sistemdeki son aktif SystemAdmin kullanıcısı pasifleştirilemez." });
        }

        user.IsActive = false;
        user.AuthVersion += 1;
        await _userManager.UpdateAsync(user);

        // Revoke all refresh tokens
        var tokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == id && !rt.IsRevoked).ToListAsync(cancellationToken);
        foreach (var t in tokens) { t.IsRevoked = true; t.RevokedAtUtc = DateTime.UtcNow; }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _cache.Remove($"UserAuthStatus:{id}");
        _cache.Remove($"UserPermissions:{id}");

        await _auditLogService.LogAsync(
            action: "User.Disabled",
            entityType: "User",
            entityId: id.ToString(),
            actorUserId: currentActorId,
            actorUsername: User.Identity?.Name,
            cancellationToken: cancellationToken);

        return Ok(new { message = "Kullanıcı başarıyla pasifleştirildi." });
    }

    [HttpPost("{id:guid}/enable")]
    [Authorize(Policy = "users.disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EnableUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.IsActive = true;
        user.AuthVersion += 1;
        await _userManager.UpdateAsync(user);

        _cache.Remove($"UserAuthStatus:{id}");

        await _auditLogService.LogAsync(
            action: "User.Enabled",
            entityType: "User",
            entityId: id.ToString(),
            actorUserId: GetCurrentUserId(),
            actorUsername: User.Identity?.Name,
            cancellationToken: cancellationToken);

        return Ok(new { message = "Kullanıcı başarıyla aktif edildi." });
    }

    [HttpPost("{id:guid}/reset-password")]
    [Authorize(Policy = "users.edit")]
    [ProducesResponseType(typeof(ResetPasswordResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        var tempPassword = GenerateCryptographicPassword(16);
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, tempPassword);

        if (!resetResult.Succeeded)
        {
            return BadRequest(new { detail = string.Join(", ", resetResult.Errors.Select(e => e.Description)) });
        }

        user.MustChangePassword = true;
        user.AuthVersion += 1;
        await _userManager.UpdateAsync(user);

        // Revoke all refresh tokens
        var tokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == id && !rt.IsRevoked).ToListAsync(cancellationToken);
        foreach (var t in tokens) { t.IsRevoked = true; t.RevokedAtUtc = DateTime.UtcNow; }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _cache.Remove($"UserAuthStatus:{id}");
        _cache.Remove($"UserPermissions:{id}");

        await _auditLogService.LogAsync(
            action: "User.PasswordReset",
            entityType: "User",
            entityId: id.ToString(),
            actorUserId: GetCurrentUserId(),
            actorUsername: User.Identity?.Name,
            metadata: new { note = "Temporary password generated, tokens revoked" },
            cancellationToken: cancellationToken);

        return Ok(new ResetPasswordResponseDto(tempPassword, "Geçici parola üretildi. Kullanıcı ilk girişinde parola değiştirmek zorundadır."));
    }

    [HttpPost("{id:guid}/revoke-sessions")]
    [Authorize(Policy = "users.edit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeSessions(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.AuthVersion += 1;
        await _userManager.UpdateAsync(user);

        var tokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == id && !rt.IsRevoked).ToListAsync(cancellationToken);
        foreach (var t in tokens) { t.IsRevoked = true; t.RevokedAtUtc = DateTime.UtcNow; }
        await _dbContext.SaveChangesAsync(cancellationToken);

        _cache.Remove($"UserAuthStatus:{id}");
        _cache.Remove($"UserPermissions:{id}");

        await _auditLogService.LogAsync(
            action: "User.RevokeSessions",
            entityType: "User",
            entityId: id.ToString(),
            actorUserId: GetCurrentUserId(),
            actorUsername: User.Identity?.Name,
            cancellationToken: cancellationToken);

        return Ok(new { message = "Kullanıcının tüm aktif oturumları kapatıldı." });
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private async Task<bool> IsLastActiveSystemAdminAsync(Guid userId)
    {
        var adminRole = await _roleManager.FindByNameAsync("SystemAdmin");
        if (adminRole == null) return false;

        var activeAdminCount = await (from ur in _dbContext.UserRoles
                                      join u in _dbContext.Users on ur.UserId equals u.Id
                                      where ur.RoleId == adminRole.Id && u.IsActive
                                      select u.Id).CountAsync();

        if (activeAdminCount <= 1)
        {
            var isUserAdmin = await _userManager.IsInRoleAsync(new ApplicationUser { Id = userId }, "SystemAdmin");
            return isUserAdmin;
        }

        return false;
    }

    private async Task<UserDto> BuildUserDtoAsync(ApplicationUser user, List<string> roles)
    {
        var roleEntities = await _roleManager.Roles.Where(r => roles.Contains(r.Name!)).ToListAsync();
        var roleIds = roleEntities.Select(r => r.Id).ToList();

        var permissions = await (from rp in _dbContext.RolePermissions
                             where roleIds.Contains(rp.RoleId)
                             join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                             select p.Code).Distinct().ToListAsync();

        return new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FullName,
            user.IsActive,
            user.MustChangePassword,
            user.LastLoginUtc,
            user.CreatedAtUtc,
            roles,
            permissions
        );
    }

    private static string GenerateCryptographicPassword(int length)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var bytes = new byte[length];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        var result = new char[length];
        result[0] = upper[bytes[0] % upper.Length];
        result[1] = lower[bytes[1] % lower.Length];
        result[2] = digits[bytes[2] % digits.Length];
        result[3] = special[bytes[3] % special.Length];

        for (int i = 4; i < length; i++)
        {
            result[i] = all[bytes[i] % all.Length];
        }

        for (int i = length - 1; i > 0; i--)
        {
            int j = bytes[i] % (i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return new string(result);
    }
}
