using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

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
        _userManager = userManager;

        _signInService = signInService;

        _logger = logger;
    }

    public async Task<ApplicationUser> AuthenticateWithPasswordAsync(
        string email,
        string password)
    {
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        var user = await _userManager.FindByEmailAsync(
            normalizedEmail);

        if (user is null ||
            user.Deleted)
        {
            _logger.LogWarning(
                "Intento de login con credenciales inválidas: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        var passwordIsCorrect =
            await _signInService.CheckPassword(
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

    public async Task EnsureRoleAsync(
        ApplicationUser user,
        string requiredRole)
    {
        var hasRequiredRole =
            await _userManager.IsInRoleAsync(
                user,
                requiredRole);

        if (!hasRequiredRole)
        {
            _logger.LogWarning(
                "Usuario sin el rol {Role} intentó ingresar: {Email}",
                requiredRole,
                user.Email);

            throw new AuthenticationException();
        }
    }

    public async Task<ApplicationUser?> FindByEmailAsync(
        string email)
    {
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        return await _userManager.FindByEmailAsync(
            normalizedEmail);
    }

    public async Task<ApplicationUser> CreateWithoutPasswordAsync(
        string email,
        string role)
    {
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        var now = DateTime.Now;

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = false,
            Deleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

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

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                role);

        if (!roleResult.Succeeded)
        {
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
