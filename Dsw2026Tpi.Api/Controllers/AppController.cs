using Dsw2026Tpi.Api.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dsw2026Tpi.Api.Controllers;

/// <summary>
/// Clase base para configuraciones generales de controladores
/// </summary>
[ApiController]
[EnableRateLimiting(RateLimitPolicies.General)]
public abstract class AppController : ControllerBase
{
}

