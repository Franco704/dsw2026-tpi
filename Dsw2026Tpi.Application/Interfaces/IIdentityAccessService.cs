using Dsw2026Tpi.Data.Identity;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IIdentityAccessService
{
    Task<ApplicationUser> AuthenticateWithPasswordAsync(
        string email,
        string password);

    Task EnsureRoleAsync(
        ApplicationUser user,
        string requiredRole);

    Task<ApplicationUser?> FindByEmailAsync(
        string email);

    Task<ApplicationUser> CreateWithoutPasswordAsync(
        string email,
        string role);
}