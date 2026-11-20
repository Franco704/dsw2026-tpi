using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dsw2026Tpi.Application.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    public JwtService(
        IOptions<JwtSettings> settings,
        TimeProvider timeProvider)
    {
        _settings = settings.Value;
        _timeProvider = timeProvider;
    }

    public string GenerateToken(
        string username,
        string? role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_settings.ExpiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
