using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IIdentityAccessService
{
    Task<UserInfo> AuthenticateUserWithPasswordAsync(
        string email,
        string password);

    Task EnsureUserHasRoleAsync(
        UserInfo user,
        string requiredRole);

    Task<UserInfo?> FindUserByEmailAsync(
        string email);

    Task<UserInfo> CreateUserAsync(
        string email,
        string role,
        string? password = null);
}