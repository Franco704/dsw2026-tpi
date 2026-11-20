namespace Dsw2026Tpi.Application.Dtos;

public record UserInfo(
    string Id,
    string? Email,
    string? UserName,
    bool Deleted);
