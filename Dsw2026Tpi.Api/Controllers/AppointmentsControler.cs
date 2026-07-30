using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/appointments")]

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
    [Authorize(Policy = Policies.PatientPolicy)]
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
    [Authorize(Policy = Policies.PatientPolicy)]
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
    [Authorize(Policy = Policies.PatientPolicy)]
    public async Task<IActionResult> Cancel(
        Guid appointmentId)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        await _appointmentService.CancelAsync(
            appointmentId,
            authenticatedEmail);

        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(
    typeof(Pagination<AppointmentModel.SearchResponse>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByDate(
    [FromQuery] DateTime date,
    [FromQuery] int pageSize = 10,
    [FromQuery] int pageIndex = 1)
    {
        var request = new AppointmentModel.SearchRequest(
            SpecialtyId: null,
            DoctorId: null,
            Dni: null,
            Date: date,
            PageSize: pageSize,
            PageIndex: pageIndex);

        var appointments =
            await _appointmentService.SearchAsync(request);

        return Ok(appointments);
    }
    [HttpGet("search")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(
    typeof(Pagination<AppointmentModel.SearchResponse>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
    [FromQuery] Guid? specialtyId,
    [FromQuery] Guid? doctorId,
    [FromQuery] long? dni,
    [FromQuery] DateTime? date,
    [FromQuery] int pageSize = 10,
    [FromQuery] int pageIndex = 1)
    {
        var request = new AppointmentModel.SearchRequest(
            specialtyId,
            doctorId,
            dni,
            date,
            pageSize,
            pageIndex);

        var appointments =
            await _appointmentService.SearchAsync(request);

        return Ok(appointments);
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