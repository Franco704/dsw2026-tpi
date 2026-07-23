using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("appointments")]
[Authorize(Policy = Policies.PatientPolicy)]
public class AppointmentsController : AppController
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(
        IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet("patient/{dni:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveByPatientDni(
        [FromRoute] long dni)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        var appointments = await _appointmentService
            .GetActiveByPatientDniAsync(
                dni,
                authenticatedEmail);

        return Ok(appointments);
    }

    [HttpDelete("{appointmentId:guid}")]
    //ProducesResponseType indica que codigos se debe devolver en cada endpoint donde se lo utilice
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid appointmentId)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        await _appointmentService.CancelAsync(
            appointmentId,
            authenticatedEmail);

        return NoContent();
    }

    private string GetAuthenticatedEmail()
    {
        var authenticatedEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(authenticatedEmail))
        {
            throw new AuthenticationException();
        }

        return authenticatedEmail;
    }
}