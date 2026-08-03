using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

/// <summary>
/// Encapsula las operaciones de autenticación y administración
/// de usuarios realizadas mediante ASP.NET Core Identity.
/// </summary>
public class IdentityAccessService : IIdentityAccessService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInService;
    private readonly ILogger<IdentityAccessService> _logger;

    /// <summary>
    /// Inicializa el servicio con los componentes necesarios
    /// para consultar usuarios, verificar contraseñas y registrar eventos.
    /// </summary>
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

    /// <summary>
    /// Autentica un usuario mediante email y contraseña.
    /// </summary>
    public async Task<ApplicationUser> AuthenticateWithPasswordAsync(
        string email,
        string password)
    {
        // Normaliza el email utilizado durante la autenticación.
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        // Busca al usuario por email utilizando Identity.
        var user = await _userManager.FindByEmailAsync(
            normalizedEmail);

        /*
         * Rechaza usuarios inexistentes o eliminados utilizando
         * un mensaje genérico para no revelar información.
         */
        if (user is null ||
            user.Deleted)
        {
            _logger.LogWarning(
                "Intento de login con credenciales inválidas: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        // Compara la contraseña con el hash almacenado.
        var passwordIsCorrect =
            await _signInService.CheckPasswordAsync(
                user,
                password);

        if (!passwordIsCorrect)
        {
            _logger.LogWarning(
                "Intento de login con credenciales inválidas: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        return user;
    }

    /// <summary>
    /// Verifica que el usuario posea el rol requerido.
    /// </summary>
    public async Task EnsureRoleAsync(
        ApplicationUser user,
        string requiredRole)
    {
        // Consulta si el usuario tiene el rol solicitado.
        var hasRequiredRole =
            await _userManager.IsInRoleAsync(
                user,
                requiredRole);

        /*
         * Mantiene AuthenticationException porque este método
         * se utiliza principalmente dentro del flujo de login.
         */
        if (!hasRequiredRole)
        {
            _logger.LogWarning(
                "Usuario sin el rol {Role} intentó ingresar: {Email}",
                requiredRole,
                user.Email);

            throw new AuthenticationException();
        }
    }

    /// <summary>
    /// Busca un usuario de Identity mediante su email.
    /// </summary>
    /// <returns>
    /// El usuario encontrado o null cuando no existe.
    /// </returns>
    public async Task<ApplicationUser?> FindByEmailAsync(
        string email)
    {
        // Normaliza el email antes de consultar Identity.
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        return await _userManager.FindByEmailAsync(
            normalizedEmail);
    }

    /// <summary>
    /// Crea un usuario de Identity sin contraseña
    /// y le asigna el rol indicado.
    /// </summary>
    public async Task<ApplicationUser> CreateWithoutPasswordAsync(
        string email,
        string role)
    {
        // Normaliza el email antes de almacenarlo.
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        // Registra la fecha una sola vez para mantener consistencia.
        var now = DateTime.Now;

        // Construye un usuario que no utiliza contraseña.
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = false,
            Deleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Crea el usuario sin ejecutar políticas de contraseña.
        var creationResult =
            await _userManager.CreateAsync(user);

        if (!creationResult.Succeeded)
        {
            _logger.LogError(
                "No se pudo crear el usuario sin contraseña: {Email}. " +
                "Errores: {Errors}",
                normalizedEmail,
                string.Join(
                    ", ",
                    creationResult.Errors.Select(
                        error => error.Code)));

            throw new ConflictException(
                ErrorCodes.REGISTER_USER_CONFLICT,
                nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                .WithDetail(
                    creationResult.Errors.Select(
                        error =>
                            (
                                error.Code,
                                error.Description
                            )));
        }

        // Asigna el rol indicado al usuario creado.
        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                role);

        if (!roleResult.Succeeded)
        {
            /*
             * Intenta revertir la creación para evitar dejar
             * un usuario sin el rol requerido.
             */
            var rollbackResult =
                await _userManager.DeleteAsync(user);

            _logger.LogError(
                "No se pudo asignar el rol {Role} al usuario {Email}. " +
                "Reversión exitosa: {RollbackSucceeded}. " +
                "Errores: {Errors}",
                role,
                normalizedEmail,
                rollbackResult.Succeeded,
                string.Join(
                    ", ",
                    roleResult.Errors.Select(
                        error => error.Code)));

            throw new ConflictException(
                ErrorCodes.REGISTER_USER_CONFLICT,
                nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                .WithDetail(
                    roleResult.Errors.Select(
                        error =>
                            (
                                error.Code,
                                error.Description
                            )));
        }

        return user;
    }
}
