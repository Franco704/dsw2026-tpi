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

/// <summary>
/// Gestiona los casos de uso de autenticación y registro
/// de administradores y pacientes.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IIdentityAccessService _identityAccessService;
    private readonly IPatientAccessService _patientAccessService;

    /// <summary>
    /// Inicializa el servicio con los componentes necesarios
    /// para autenticar usuarios, generar tokens y registrar operaciones.
    /// </summary>
    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        IIdentityAccessService identityAccessService,
        IPatientAccessService patientAccessService,
        JwtService jwtService,
        ILogger<AuthenticationService> logger)
    {
        // Se mantiene únicamente para el registro temporal.
        _userManager = userManager;

        // Encapsula el acceso a ASP.NET Core Identity.
        _identityAccessService = identityAccessService;

        // Encapsula la autenticación y creación de pacientes.
        _patientAccessService = patientAccessService;

        // Genera los tokens JWT de la aplicación.
        _jwtService = jwtService;

        // Registra las operaciones de autenticación.
        _logger = logger;
    }

    /// <summary>
    /// Autentica un administrador mediante email y contraseña
    /// y genera su token JWT.
    /// </summary>
    public async Task<LoginAdminModel.Response> LoginAdminAsync(
        LoginAdminModel.Request request)
    {
        // Valida el formato y obligatoriedad del email.
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        // Valida la contraseña recibida.
        AuthenticationRequestValidator.ValidateLoginPassword(
            request.Password);

        // Autentica al usuario mediante ASP.NET Core Identity.
        var user =
            await _identityAccessService
                .AuthenticateWithPasswordAsync(
                    request.Email,
                    request.Password);

        // Verifica que el usuario pueda ingresar como administrador.
        await _identityAccessService.EnsureRoleAsync(
            user,
            Roles.Administrator);

        /*
         * Identity debería almacenar siempre un UserName.
         * Si no está configurado, el login no puede completarse.
         */
        if (string.IsNullOrWhiteSpace(user.UserName))
        {
            _logger.LogError(
                "El usuario {UserId} no posee UserName configurado.",
                user.Id);

            throw new AuthenticationException();
        }

        // Genera el JWT con el rol administrativo.
        var token = _jwtService.GenerateToken(
            user.UserName,
            Roles.Administrator);

        // Registra el acceso exitoso sin incluir credenciales.
        _logger.LogInformation(
            "Login de administrador exitoso. UserId: {UserId}",
            user.Id);

        return new LoginAdminModel.Response(
            token,
            Roles.Administrator.ToUpperInvariant());
    }

    /// <summary>
    /// Autentica un paciente mediante email y DNI.
    /// Si es su primer acceso, crea el usuario y el paciente.
    /// </summary>
    public async Task<LoginPatientModel.Response> LoginPatientAsync(
        LoginPatientModel.Request request)
    {
        // Valida el formato y obligatoriedad del email.
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        // Valida el formato permitido para el DNI.
        AuthenticationRequestValidator.ValidatePatientDni(
            request.Dni);

        // Normaliza el email antes de buscarlo o almacenarlo.
        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        // Autentica al paciente o lo crea durante el primer acceso.
        var patient =
            await _patientAccessService
                .AuthenticateOrCreateAsync(
                    normalizedEmail,
                    request.Dni);

        // Genera el JWT con el rol utilizado por PatientPolicy.
        var token = _jwtService.GenerateToken(
            normalizedEmail,
            Roles.Patient);

        // Registra el acceso sin almacenar el DNI ni el token.
        _logger.LogInformation(
            "Login de paciente exitoso. PatientId: {PatientId}",
            patient.Id);

        return new LoginPatientModel.Response(
            token,
            Roles.Patient.ToUpperInvariant());
    }

    /// <summary>
    /// Registra temporalmente un usuario administrador
    /// y le asigna el rol correspondiente.
    /// </summary>
    public async Task<RegisterModel.Response> RegisterAsync(
        RegisterModel.Request request)
    {
        // Valida el formato del email recibido.
        if (!request.Email.IsEmailValid())
        {
            throw new ValidationException(
                ErrorCodes.REGISTER_USER_INVALID,
                nameof(ErrorCodes.REGISTER_USER_INVALID));
        }

        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        var now = DateTime.Now;

        // Construye el usuario que será administrado por Identity.
        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            CreatedAt = now,
            UpdatedAt = now,
            Deleted = false
        };

        // Crea el usuario aplicando las políticas de contraseña.
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

        // Asigna el rol administrativo al usuario creado.
        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                Roles.Administrator);

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
