using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/auth")]
public class AuthenticationController : AppController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }



    [AllowAnonymous]
    [HttpPost("admin/register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterModel.Request request)
    {
        // Delega la creación del usuario al servicio de autenticación.
        var result =
            await _authenticationService.Register(request);

        // Devuelve el email del administrador creado.
        return Ok(result.Email);
    }
        

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AdminLogin)]
    [HttpPost("admin/login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginAdminModel.Request request)
    {
        // Delega la autenticación al servicio.
        var result =
            await _authenticationService.LoginAdmin(request);

        // Devuelve el JWT y el rol.
        return Ok(result);
    }
    // Permite el primer acceso y el login de pacientes sin JWT previo.
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PatientLogin)]
    [HttpPost("patient/login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LoginPatient(
        [FromBody] LoginPatientModel.Request request)
    {
        // Delega el flujo de autenticación a Application.
        var result = await _authenticationService
            .LoginPatient(request);

        // Devuelve el JWT y el rol del paciente.
        return Ok(result);
    }
}
//IActionResult permite devolver distintos resultados HTTP, como Ok(), BadRequest() o Unauthorized()
//[FromBody] hace que ASP.NET Core deserialice el JSON y construya el DTO Request.