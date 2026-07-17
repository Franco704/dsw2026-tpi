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
        // Todavía se utiliza para el registro temporal.
        _userManager = userManager;

        // Encapsula la autenticación y verificación de roles.
        _identityAccessService = identityAccessService;

        // Genera los tokens JWT.
        _jwtService = jwtService;

        // Registra las operaciones del caso de uso.
        _logger = logger;
        _patientAccessService = patientAccessService;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(
    LoginAdminModel.Request request)
    {
        // Valida los datos recibidos.
        AuthenticationRequestValidator.ValidateEmail(request.Email);
        AuthenticationRequestValidator.ValidateLoginPassword(request.Password);

        // Autentica al usuario mediante Identity.
        var user = await _identityAccessService.AuthenticateWithPasswordAsync(
            request.Email,
            request.Password);

        // Verifica que pueda ingresar como administrador.
        await _identityAccessService.EnsureRoleAsync(
            user,
            Roles.Administrator);

        // Comprueba que Identity tenga un UserName configurado.
        var username = user.UserName
            ?? throw new InvalidOperationException(
                "El usuario no tiene UserName configurado.");

        // Genera el JWT con el rol administrativo.
        var token = _jwtService.GenerateToken(
            username,
            Roles.Administrator);

        // Registra el login exitoso.
        _logger.LogInformation(
            "Login de administrador exitoso: {Email}",
            request.Email);

        // Devuelve el formato solicitado.
        return new LoginAdminModel.Response(
            token,
            Roles.Administrator.ToUpperInvariant());
    }

    public async Task<LoginPatientModel.Response> LoginPatient(
        LoginPatientModel.Request request)
    {
        // Valida que el email sea obligatorio y tenga formato válido.
        AuthenticationRequestValidator.ValidateEmail(
            request.Email);

        // Valida que el DNI tenga siete u ocho dígitos.
        AuthenticationRequestValidator.ValidatePatientDni(
            request.Dni);

        // Normaliza el email para almacenarlo y utilizarlo en el JWT.
        var normalizedEmail = request.Email
            .Trim()
            .ToLowerInvariant();

        // Autentica al paciente o lo crea durante su primer acceso.
        var patient = await _patientAccessService
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

        // Devuelve el formato exigido por la consigna.
        return new LoginPatientModel.Response(
            token,
            Roles.Patient.ToUpperInvariant());
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new ValidationException(ErrorCodes.REGISTER_USER_INVALID,
            nameof(ErrorCodes.REGISTER_USER_INVALID));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) throw new ConflictException(nameof(ErrorCodes.REGISTER_USER_CONFLICT),
            ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));
       
        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
