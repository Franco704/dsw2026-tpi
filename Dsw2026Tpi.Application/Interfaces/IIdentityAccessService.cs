using Dsw2026Tpi.Data.Identity;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IIdentityAccessService
{
    Task<ApplicationUser> AuthenticateUserWithPasswordAsync(
        string email,
        string password);

    Task EnsureUserHasRoleAsync(
        ApplicationUser user,
        string requiredRole);

    Task<ApplicationUser?> FindUserByEmailAsync(
        string email);

    Task<ApplicationUser> CreateUserWithoutPasswordAsync(
        string email,
        string role);
}