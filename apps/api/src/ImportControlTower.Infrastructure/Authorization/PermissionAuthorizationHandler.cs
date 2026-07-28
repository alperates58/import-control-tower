using System.Security.Claims;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportControlTower.Infrastructure.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public PermissionAuthorizationHandler(ApplicationDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var authVersionClaim = context.User.FindFirst("auth_version")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        // 1. Financial Privacy Guard: If financial requirement, check FinancialModuleEnabled
        if (requirement.PermissionCode == PermissionsCatalog.FinancialView || 
            requirement.PermissionCode == PermissionsCatalog.FinancialEdit)
        {
            var isFinancialEnabled = await _cache.GetOrCreateAsync("SystemSetting:FinancialModuleEnabled", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                var setting = await _dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "FinancialModuleEnabled");
                return setting != null && bool.TryParse(setting.Value, out var val) && val;
            });

            if (!isFinancialEnabled)
            {
                // Module is disabled system-wide. Block access even for SystemAdmin / Finance.
                return;
            }
        }

        // 2. Fetch User & AuthVersion
        var userCacheKey = $"UserAuthStatus:{userId}";
        var userStatus = await _cache.GetOrCreateAsync(userCacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromSeconds(30);
            var u = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            if (u == null) return null;
            return new { u.IsActive, u.MustChangePassword, u.AuthVersion };
        });

        if (userStatus == null || !userStatus.IsActive)
        {
            return; // Inactive or deleted user
        }

        if (userStatus.MustChangePassword)
        {
            return; // Password change required before accessing protected business endpoints
        }

        if (!int.TryParse(authVersionClaim, out var tokenAuthVersion) || tokenAuthVersion < userStatus.AuthVersion)
        {
            return; // Outdated auth_version
        }

        // 3. Check User Permissions
        var permissionsCacheKey = $"UserPermissions:{userId}";
        var userPermissions = await _cache.GetOrCreateAsync(permissionsCacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(1);

            var roles = await (from ur in _dbContext.UserRoles
                               where ur.UserId == userId
                               join r in _dbContext.Roles on ur.RoleId equals r.Id
                               select r).ToListAsync();

            if (roles.Any(r => r.Name == "SystemAdmin"))
            {
                return PermissionsCatalog.All.Select(p => p.Code).ToHashSet();
            }

            var roleIds = roles.Select(r => r.Id).ToList();
            var perms = await (from rp in _dbContext.RolePermissions
                               where roleIds.Contains(rp.RoleId)
                               join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                               select p.Code).Distinct().ToListAsync();

            return perms.ToHashSet();
        });

        if (userPermissions != null && userPermissions.Contains(requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }
    }
}
