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
    public async Task<ApplicationUser?> FindByEmailAsync(
    string email)
    {
        // Utiliza la búsqueda y normalización de Identity.
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser> CreateWithoutPasswordAsync(
        string email,
        string role)
    {
        // Registra la fecha una sola vez para mantener consistencia.
        var now = DateTime.UtcNow;

        // Construye un usuario que no utiliza contraseña.
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            Deleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Crea el usuario sin ejecutar las políticas de contraseña.
        var creationResult = await _userManager.CreateAsync(user);

        // Rechaza la operación si Identity no pudo crear al usuario.
        if (!creationResult.Succeeded)
        {
            _logger.LogError(
                "No se pudo crear el usuario sin contraseña: {Email}. Errores: {Errors}",
                email,
                string.Join(
                    ", ",
                    creationResult.Errors.Select(error => error.Code)));

            // El middleware convertirá este error inesperado en 500.
            throw new InvalidOperationException(
                "No se pudo crear el usuario de Identity.");
        }

        // Asigna el rol indicado al usuario creado.
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            role);

        // Revierte la creación si no fue posible asignar el rol.
        if (!roleResult.Succeeded)
        {
            // El usuario acaba de crearse dentro de esta operación.
            await _userManager.DeleteAsync(user);

            _logger.LogError(
                "No se pudo asignar el rol {Role} al usuario {Email}.",
                role,
                email);

            throw new InvalidOperationException(
                "No se pudo asignar el rol al usuario.");
        }

        // Retorna el usuario correctamente creado.
        return user;
    }
}