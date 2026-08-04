using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IIdentityAccessService _identityAccessService;
    private readonly IPatientAccessService _patientAccessService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IIdentityAccessService identityAccessService,
        IPatientAccessService patientAccessService,
        JwtService jwtService,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;

        _identityAccessService = identityAccessService;

        _patientAccessService = patientAccessService;

        _jwtService = jwtService;

        _logger = logger;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(
        LoginAdminModel.Request request)
    {
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        AuthenticationRequestValidator.ValidateLoginPassword(
            request.Password);

        var user =
            await _identityAccessService
                .AuthenticateWithPasswordAsync(
                    request.Email,
                    request.Password);

        await _identityAccessService.EnsureRoleAsync(
            user,
            Roles.Administrator);

        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            _logger.LogError(
                "El usuario {UserId} no posee UserName configurado.",
                user.Id);

            throw new AuthenticationException();
        }

        var token = _jwtService.GenerateToken(
            user.UserName,
            Roles.Administrator);

        _logger.LogInformation(
            "Login de administrador exitoso. UserId: {UserId}",
            user.Id);

        return new LoginAdminModel.Response(
            token,
            Roles.Administrator.ToUpperInvariant());
    }

    public async Task<LoginPatientModel.Response> LoginPatient(
        LoginPatientModel.Request request)
    {
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        AuthenticationRequestValidator.ValidatePatientDni(
            request.Dni);

        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        var patient =
            await _patientAccessService
                .AuthenticateOrCreateAsync(
                    normalizedEmail,
                    request.Dni);

        var token = _jwtService.GenerateToken(
            normalizedEmail,
            Roles.Patient);

        _logger.LogInformation(
            "Login de paciente exitoso. PatientId: {PatientId}",
            patient.Id);

        return new LoginPatientModel.Response(
            token,
            Roles.Patient.ToUpperInvariant());
    }

    public async Task<RegisterModel.Response> Register(
        RegisterModel.Request request)
    {
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        var now = DateTime.Now;

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            CreatedAt = now,
            UpdatedAt = now,
            Deleted = false
        };

        var creationResult =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!creationResult.Succeeded)
        {
            _logger.LogWarning(
                "No se pudo registrar el usuario {Email}. Errores: {Errors}",
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
                Roles.Administrator);

        if (!roleResult.Succeeded)
        {
            var rollbackResult =
                await _userManager.DeleteAsync(user);

            _logger.LogError(
                "No se pudo asignar el rol {Role} al usuario {Email}. " +
                "Reversión exitosa: {RollbackSucceeded}. Errores: {Errors}",
                Roles.Administrator,
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

        _logger.LogInformation(
            "Usuario administrador registrado correctamente. UserId: {UserId}",
            user.Id);

        return new RegisterModel.Response(
            normalizedEmail);
    }
}
