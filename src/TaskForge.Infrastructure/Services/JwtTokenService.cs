using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Shared.Constants;

namespace TaskForge.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenGenerator
{
    public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiryUtc) GenerateTokens(
        Guid userId, string email, string role, Guid tenantId, string schemaName)
    {
        var secretKey = configuration["Jwt:Secret"]
            ?? configuration["JwtSettings:SecretKey"]
            ?? "SuperSecretKeyForTaskForgeBackend1234567890!";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
            new Claim(TenantConstants.ClaimName, tenantId.ToString()),
            new Claim(TenantConstants.SchemaClaimName, schemaName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiryMinutesStr = configuration["Jwt:ExpiryMinutes"] ?? configuration["JwtSettings:ExpiryMinutes"] ?? "120";
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var minutes) ? minutes : 120;

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"] ?? "TaskForgeApi",
            audience: configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"] ?? "TaskForgeClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        
        // Cryptographically secure random token for the client
        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        
        // Compute SHA-256 hash for secure database persistence
        var hashedRefreshToken = HashToken(rawRefreshToken);

        var expiry = DateTime.UtcNow.AddDays(7);

        // Return the raw token for the client, while RegisterTenant / auth stores hashedRefreshToken
        return (accessToken, rawRefreshToken, expiry);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
