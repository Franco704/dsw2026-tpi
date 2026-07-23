using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Models;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
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

    [HttpPost]
    [ProducesResponseType(
        typeof(AppointmentModel.Response),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status404NotFound)]
    //Entra en conflicto con el slot de la cita
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] AppointmentModel.Request request)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        var appointment = await _appointmentService.CreateAsync(
            request,
            authenticatedEmail);

        return Created(
            $"/api/appointments/{appointment.Id}",
            appointment);
    }

    [HttpGet("patient")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<AppointmentModel.Response>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetActiveByPatientDni(
        [FromQuery] long dni)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        var appointments = await _appointmentService
            .GetActiveByPatientDniAsync(
                dni,
                authenticatedEmail);

        return Ok(appointments);
    }

    [HttpDelete("{appointmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid appointmentId)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        await _appointmentService.CancelAsync(
            appointmentId,
            authenticatedEmail);

        return NoContent();
    }
    private string GetAuthenticatedEmail()
    {
        //Funcion para obtener el email del usuario autenticado desde el token JWT, si no esta autenticado devuelve una excepcion de autenticacion
        var authenticatedEmail = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(authenticatedEmail))
        {
            throw new AuthenticationException();
        }

        return authenticatedEmail;
    }
}