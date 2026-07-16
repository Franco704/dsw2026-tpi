using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

// Encapsula las operaciones de autenticación realizadas con Identity.
public class IdentityAccessService : IIdentityAccessService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInService;
    private readonly ILogger<IdentityAccessService> _logger;

    public IdentityAccessService(
        UserManager<ApplicationUser> userManager,
        ISignInService signInService,
        ILogger<IdentityAccessService> logger)
    {
        // Permite buscar usuarios y consultar sus roles.
        _userManager = userManager;

        // Permite comprobar contraseñas mediante Identity.
        _signInService = signInService;

        // Permite registrar los intentos de autenticación.
        _logger = logger;
    }

    public async Task<ApplicationUser> AuthenticateWithPasswordAsync(
        string email,
        string password)
    {
        // Busca al usuario por email utilizando Identity.
        var user = await _userManager.FindByEmailAsync(email);

        // Rechaza usuarios inexistentes o eliminados.
        if (user is null || user.Deleted)
        {
            _logger.LogWarning(
                "Intento de login con credenciales inválidas: {Email}",
                email);

            // No informa si el usuario existe o está eliminado.
            throw new AuthenticationException();
        }

        // Compara la contraseña con el hash almacenado.
        var passwordIsCorrect = await _signInService.CheckPassword(
            user,
            password);

        // Rechaza una contraseña incorrecta.
        if (!passwordIsCorrect)
        {
            _logger.LogWarning(
                "Intento de login con credenciales inválidas: {Email}",
                email);

            throw new AuthenticationException();
        }

        // Retorna el usuario correctamente autenticado.
        return user;
    }

    public async Task EnsureRoleAsync(
        ApplicationUser user,
        string requiredRole)
    {
        // Consulta si el usuario tiene el rol requerido.
        var hasRequiredRole = await _userManager.IsInRoleAsync(
            user,
            requiredRole);

        // Rechaza el acceso cuando el rol no corresponde.
        if (!hasRequiredRole)
        {
            _logger.LogWarning(
                "Usuario sin el rol {Role} intentó ingresar: {Email}",
                requiredRole,
                user.Email);

            // Mantiene un error genérico dentro del flujo de login.
            throw new AuthenticationException();
        }
    }
}