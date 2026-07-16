using Dsw2026Tpi.Data.Identity;

namespace Dsw2026Tpi.Application.Interfaces;

// Define las operaciones comunes realizadas mediante Identity.
public interface IIdentityAccessService
{
    // Autentica un usuario mediante email y contraseña.
    Task<ApplicationUser> AuthenticateWithPasswordAsync(
        string email,
        string password);

    // Comprueba que el usuario tenga un rol determinado.
    Task EnsureRoleAsync(
        ApplicationUser user,
        string requiredRole);
}