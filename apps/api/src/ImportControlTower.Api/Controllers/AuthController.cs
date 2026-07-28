using System.Security.Claims;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditLogService _auditLogService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IJwtTokenService jwtTokenService,
        IAuditLogService auditLogService,
        ApplicationDbContext dbContext,
        IMemoryCache cache,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtTokenService = jwtTokenService;
        _auditLogService = auditLogService;
        _dbContext = dbContext;
        _cache = cache;
        _environment = environment;
        _configuration = configuration;
    }

    private string CookieName => _environment.IsProduction() ? "__Host-ict_refresh_token" : "ict_refresh_token";

    [HttpPost("login")]
    [EnableRateLimiting("login-policy")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.UsernameOrEmail) 
            ?? await _userManager.FindByNameAsync(request.UsernameOrEmail);

        if (user == null || !user.IsActive)
        {
            await _auditLogService.LogAsync(
                action: "Auth.LoginFailed",
                entityType: "User",
                entityId: "Unknown",
                actorType: "Anonymous",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers.UserAgent.ToString(),
                metadata: new { maskedEmail = MaskEmail(request.UsernameOrEmail) },
                cancellationToken: cancellationToken);

            return Unauthorized(new { detail = "Giriş bilgileri geçersiz." });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Unauthorized(new { detail = "Giriş bilgileri geçersiz." });
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            await _auditLogService.LogAsync(
                action: "Auth.LoginFailed",
                entityType: "User",
                entityId: user.Id.ToString(),
                actorUserId: user.Id,
                actorUsername: user.UserName,
                actorType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers.UserAgent.ToString(),
                metadata: new { maskedEmail = MaskEmail(user.Email) },
                cancellationToken: cancellationToken);

            return Unauthorized(new { detail = "Giriş bilgileri geçersiz." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roles);

        // Generate Refresh Token
        var plainRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var hashedRefreshToken = _jwtTokenService.HashRefreshToken(plainRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            FamilyId = Guid.NewGuid(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        SetRefreshTokenCookie(plainRefreshToken);

        await _auditLogService.LogAsync(
            action: "Auth.LoginSuccess",
            entityType: "User",
            entityId: user.Id.ToString(),
            actorUserId: user.Id,
            actorUsername: user.UserName,
            actorType: "User",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers.UserAgent.ToString(),
            cancellationToken: cancellationToken);

        var userDto = await BuildUserDtoAsync(user, roles.ToList());
        return Ok(new AuthResponseDto(accessToken, DateTime.UtcNow.AddMinutes(15), userDto));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("refresh-policy")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(CookieName, out var plainRefreshToken) || string.IsNullOrEmpty(plainRefreshToken))
        {
            return Unauthorized(new { detail = "Refresh token cookie not found." });
        }

        var hashedToken = _jwtTokenService.HashRefreshToken(plainRefreshToken);

        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingToken = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken, cancellationToken);

            if (existingToken == null)
            {
                return Unauthorized(new { detail = "Invalid refresh token." });
            }

            // Reuse Detection
            if (existingToken.IsRevoked)
            {
                // Revoke all tokens in family
                var familyTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.FamilyId == existingToken.FamilyId && !rt.IsRevoked)
                    .ToListAsync(cancellationToken);

                foreach (var t in familyTokens)
                {
                    t.IsRevoked = true;
                    t.RevokedAtUtc = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await _auditLogService.LogAsync(
                    action: "Auth.RefreshTokenReuseDetected",
                    entityType: "RefreshToken",
                    entityId: existingToken.FamilyId.ToString(),
                    actorUserId: existingToken.UserId,
                    actorType: "User",
                    metadata: new { message = "Revoked token family due to reuse detection" },
                    cancellationToken: cancellationToken);

                ClearRefreshTokenCookie();
                return Unauthorized(new { detail = "Security warning: Revoked token reuse detected." });
            }

            if (existingToken.ExpiresAtUtc <= DateTime.UtcNow || !existingToken.User.IsActive)
            {
                return Unauthorized(new { detail = "Expired or inactive token." });
            }

            // Revoke current token
            existingToken.IsRevoked = true;
            existingToken.RevokedAtUtc = DateTime.UtcNow;

            // Generate new token in same family
            var newPlainRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var newHashedRefreshToken = _jwtTokenService.HashRefreshToken(newPlainRefreshToken);
            existingToken.ReplacedByTokenHash = newHashedRefreshToken;

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = existingToken.UserId,
                TokenHash = newHashedRefreshToken,
                FamilyId = existingToken.FamilyId,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                IsRevoked = false
            };

            _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var roles = await _userManager.GetRolesAsync(existingToken.User);
            var newAccessToken = _jwtTokenService.GenerateAccessToken(existingToken.User, roles);

            SetRefreshTokenCookie(newPlainRefreshToken);

            var userDto = await BuildUserDtoAsync(existingToken.User, roles.ToList());
            return Ok(new AuthResponseDto(newAccessToken, DateTime.UtcNow.AddMinutes(15), userDto));
        }
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency exception safely without 500 error
            ClearRefreshTokenCookie();
            return Unauthorized(new { detail = "Concurrent refresh token request processed." });
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(CookieName, out var plainRefreshToken) && !string.IsNullOrEmpty(plainRefreshToken))
        {
            var hashedToken = _jwtTokenService.HashRefreshToken(plainRefreshToken);
            var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken, cancellationToken);
            if (token != null && !token.IsRevoked)
            {
                token.IsRevoked = true;
                token.RevokedAtUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        ClearRefreshTokenCookie();
        return Ok(new { message = "Çıkış başarılı." });
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedAtUtc = DateTime.UtcNow;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.AuthVersion += 1;
            await _userManager.UpdateAsync(user);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _cache.Remove($"UserAuthStatus:{userId}");
        _cache.Remove($"UserPermissions:{userId}");

        ClearRefreshTokenCookie();
        return Ok(new { message = "Tüm aktif oturumlar kapatıldı." });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return Unauthorized();
        }

        var authVersionClaim = User.FindFirst("auth_version")?.Value;
        if (int.TryParse(authVersionClaim, out var tokenVersion) && tokenVersion < user.AuthVersion)
        {
            return Unauthorized(new { detail = "Oturum sonlandırıldı." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = await BuildUserDtoAsync(user, roles.ToList());
        return Ok(userDto);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || !user.IsActive)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { detail = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        user.MustChangePassword = false;
        user.AuthVersion += 1;
        await _userManager.UpdateAsync(user);

        // Revoke all refresh tokens
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.IsRevoked = true;
            t.RevokedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _cache.Remove($"UserAuthStatus:{userId}");
        _cache.Remove($"UserPermissions:{userId}");

        ClearRefreshTokenCookie();
        return Ok(new { message = "Parola başarıyla değiştirildi." });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var isProd = _environment.IsProduction();
        var cookieOptions = new CookieHeaderValueOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = isProd ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/", // Path=/ is mandatory for __Host- prefix
            Expires = DateTime.UtcNow.AddDays(7)
        };

        Response.Cookies.Append(CookieName, refreshToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            Path = cookieOptions.Path,
            Expires = cookieOptions.Expires
        });
    }

    private void ClearRefreshTokenCookie()
    {
        var isProd = _environment.IsProduction();
        Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProd,
            SameSite = isProd ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/"
        });
    }

    private async Task<UserDto> BuildUserDtoAsync(ApplicationUser user, List<string> roles)
    {
        List<string> permissions;
        if (roles.Contains("SystemAdmin"))
        {
            permissions = PermissionsCatalog.All.Select(p => p.Code).ToList();
        }
        else
        {
            var roleEntities = await _roleManager.Roles.Where(r => roles.Contains(r.Name!)).ToListAsync();
            var roleIds = roleEntities.Select(r => r.Id).ToList();

            permissions = await (from rp in _dbContext.RolePermissions
                                 where roleIds.Contains(rp.RoleId)
                                 join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                                 select p.Code).Distinct().ToListAsync();
        }

        return new UserDto(
            user.Id,
            user.UserName ?? user.Email ?? string.Empty,
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

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@')) return "***";
        var parts = email.Split('@');
        var name = parts[0];
        var maskedName = name.Length > 2 ? $"{name[0]}***{name[^1]}" : "***";
        return $"{maskedName}@{parts[1]}";
    }

    private class CookieHeaderValueOptions
    {
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public SameSiteMode SameSite { get; set; }
        public string Path { get; set; } = "/";
        public DateTimeOffset Expires { get; set; }
    }
}
