using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ImportControlTower.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _pepper;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secret = _configuration["JWT_SECRET"] 
            ?? _configuration["Jwt:Secret"] 
            ?? "DefaultSuperSecretKeyThatIsAtLeast32BytesLongForHS256Algorithm2026!";

        if (_secret.Length < 32)
        {
            throw new InvalidOperationException("JWT_SECRET must be at least 32 characters long.");
        }

        _issuer = _configuration["JWT_ISSUER"] ?? _configuration["Jwt:Issuer"] ?? "ImportControlTower";
        _audience = _configuration["JWT_AUDIENCE"] ?? _configuration["Jwt:Audience"] ?? "ImportControlTowerApp";
        _pepper = _configuration["REFRESH_TOKEN_PEPPER"] ?? "DefaultSecretPepperKey2026";
    }

    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("full_name", user.FullName),
            new("auth_version", user.AuthVersion.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string token)
    {
        var saltedToken = string.Concat(token, _pepper);
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(saltedToken);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    public bool VerifyRefreshToken(string plainToken, string hashedToken)
    {
        var computedHash = HashRefreshToken(plainToken);
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);
        var targetBytes = Encoding.UTF8.GetBytes(hashedToken);
        return CryptographicOperations.FixedTimeEquals(computedBytes, targetBytes);
    }
}
