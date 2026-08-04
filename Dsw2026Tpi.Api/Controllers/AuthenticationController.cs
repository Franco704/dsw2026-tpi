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
        var result =
            await _authenticationService.Register(request);

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
        var result =
            await _authenticationService.LoginAdmin(request);

        return Ok(result);
    }
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
        var result = await _authenticationService
            .LoginPatient(request);

        return Ok(result);
    }
}