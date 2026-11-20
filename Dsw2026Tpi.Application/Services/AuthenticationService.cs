using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IIdentityAccessService _identityAccessService;
    private readonly IPatientAccessService _patientAccessService;

    public AuthenticationService(
        IIdentityAccessService identityAccessService,
        IPatientAccessService patientAccessService,
        IJwtService jwtService,
        ILogger<AuthenticationService> logger)
    {

        _identityAccessService = identityAccessService;

        _patientAccessService = patientAccessService;

        _jwtService = jwtService;

        _logger = logger;
    }

    public async Task<LoginAdminModel.Response> LoginAdminAsync(
        LoginAdminModel.Request request)
    {
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        AuthenticationRequestValidator.ValidateLoginPassword(
            request.Password);

        var user =
            await _identityAccessService
                .AuthenticateUserWithPasswordAsync(
                    request.Email,
                    request.Password);

        await _identityAccessService.EnsureUserHasRoleAsync(
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

    public async Task<LoginPatientModel.Response> LoginPatientAsync(
        LoginPatientModel.Request request)
    {
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        AuthenticationRequestValidator.ValidatePatientDni(
            request.Dni);
        
        var normalizedEmail = request.Email.NormalizeEmail();

        var patient =
            await _patientAccessService
                .AuthenticatePatientOrCreateAsync(
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

    public async Task<RegisterModel.Response> RegisterAdminAsync(
        RegisterModel.Request request)
    {
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        var user =
            await _identityAccessService.CreateUserAsync(
                request.Email,
                Roles.Administrator,
                request.Password);

        _logger.LogInformation(
            "Usuario administrador registrado correctamente. UserId: {UserId}",
            user.Id);

        return new RegisterModel.Response(
            user.Email!);
    }
}
