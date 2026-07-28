using ImportControlTower.Domain.Entities;

namespace ImportControlTower.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    bool VerifyRefreshToken(string plainToken, string hashedToken);
}
