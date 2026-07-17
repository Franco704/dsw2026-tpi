using Dsw2026Tpi.Data.Identity;

namespace Dsw2026Tpi.Application.Interfaces;

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

    // Busca un usuario de Identity mediante su email.
    Task<ApplicationUser?> FindByEmailAsync(
        string email);

    // Crea un usuario sin contraseña y le asigna un rol.
    Task<ApplicationUser> CreateWithoutPasswordAsync(
        string email,
        string role);
}