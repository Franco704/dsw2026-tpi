namespace Dsw2026Tpi.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(
        string username,
        string? role);
}
