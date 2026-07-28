using System.Security.Claims;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IMemoryCache _cache;

    public RolesController(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IAuditLogService auditLogService,
        IMemoryCache cache)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _cache = cache;
    }

    [HttpGet]
    [Authorize(Policy = "roles.view")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<RoleDto>();

        foreach (var role in roles)
        {
            var permissions = await (from rp in _dbContext.RolePermissions
                                     where rp.RoleId == role.Id
                                     join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                                     select p.Code).ToListAsync(cancellationToken);

            result.Add(new RoleDto(role.Id, role.Name!, role.Description, role.IsSystemRole, permissions));
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "roles.view")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { detail = "Rol bulunamadı." });

        var permissions = await (from rp in _dbContext.RolePermissions
                                 where rp.RoleId == role.Id
                                 join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                                 select p.Code).ToListAsync(cancellationToken);

        return Ok(new RoleDto(role.Id, role.Name!, role.Description, role.IsSystemRole, permissions));
    }

    [HttpPost]
    [Authorize(Policy = "roles.create")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        if (await _roleManager.RoleExistsAsync(request.Name))
        {
            return BadRequest(new { detail = "Bu isimde bir rol zaten mevcut." });
        }

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsSystemRole = false
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { detail = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        if (request.Permissions != null && request.Permissions.Count > 0)
        {
            var validPermissions = await _dbContext.Permissions
                .Where(p => request.Permissions.Contains(p.Code))
                .ToListAsync(cancellationToken);

            foreach (var perm in validPermissions)
            {
                _dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _auditLogService.LogAsync(
            action: "Role.Created",
            entityType: "Role",
            entityId: role.Id.ToString(),
            actorUserId: GetCurrentUserId(),
            actorUsername: User.Identity?.Name,
            metadata: new { name = role.Name, permissions = request.Permissions },
            cancellationToken: cancellationToken);

        return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, new RoleDto(role.Id, role.Name, role.Description, false, request.Permissions ?? new List<string>()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "roles.edit")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { detail = "Rol bulunamadı." });

        role.Description = request.Description;
        await _roleManager.UpdateAsync(role);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Update Role Permissions
        var currentRolePerms = await _dbContext.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(cancellationToken);
        _dbContext.RolePermissions.RemoveRange(currentRolePerms);

        var validPermissions = await _dbContext.Permissions
            .Where(p => request.Permissions.Contains(p.Code))
            .ToListAsync(cancellationToken);

        foreach (var perm in validPermissions)
        {
            _dbContext.RolePermissions.Add(new RolePermission { RoleId = id, PermissionId = perm.Id });
        }

        // Find all users holding this role
        var userIds = await (from ur in _dbContext.UserRoles
                             where ur.RoleId == id
                             select ur.UserId).ToListAsync(cancellationToken);

        // Atomic invalidation: Increment AuthVersion & Revoke Tokens
        var usersToUpdate = await _dbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
        foreach (var user in usersToUpdate)
        {
            user.AuthVersion += 1;
            _cache.Remove($"UserAuthStatus:{user.Id}");
            _cache.Remove($"UserPermissions:{user.Id}");
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => userIds.Contains(rt.UserId) && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _auditLogService.LogAsync(
            action: "Role.Updated",
            entityType: "Role",
            entityId: id.ToString(),
            actorUserId: GetCurrentUserId(),
            actorUsername: User.Identity?.Name,
            metadata: new { name = role.Name, permissions = request.Permissions, affectedUsersCount = userIds.Count },
            cancellationToken: cancellationToken);

        return Ok(new RoleDto(role.Id, role.Name!, role.Description, role.IsSystemRole, request.Permissions));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "roles.delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null) return NotFound(new { detail = "Rol bulunamadı." });

        if (role.IsSystemRole)
        {
            return Conflict(new { detail = "Sistem rolleri silinemez." });
        }

        var assignedUsersCount = await _dbContext.UserRoles.CountAsync(ur => ur.RoleId == id, cancellationToken);
        if (assignedUsersCount > 0)
        {
            return Conflict(new { detail = "Aktif kullanıcılara atanmış olan bir rol silinemez." });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { detail = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        await _auditLogService.LogAsync(
            action: "Role.Deleted",
            entityType: "Role",
            entityId: id.ToString(),
            actorUserId: GetCurrentUserId(),
            actorUsername: User.Identity?.Name,
            cancellationToken: cancellationToken);

        return Ok(new { message = "Rol başarıyla silindi." });
    }

    [HttpGet("/api/v1/admin/permissions")]
    [Authorize(Policy = "roles.view")]
    [ProducesResponseType(typeof(IEnumerable<PermissionItem>), StatusCodes.Status200OK)]
    public IActionResult GetPermissionsCatalog()
    {
        return Ok(PermissionsCatalog.All);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
