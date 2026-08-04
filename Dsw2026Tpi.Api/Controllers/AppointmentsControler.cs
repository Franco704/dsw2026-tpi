using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [Authorize(Policy = Policies.PatientPolicy)]
    [EnableRateLimiting(RateLimitPolicies.AppointmentBooking)]
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
    [ProducesResponseType(
        typeof(ErrorResponse),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] AppointmentModel.Request request)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        var appointment =
            await _appointmentService.CreateAppointmentAsync(
                request,
                authenticatedEmail);

        return Created(
            $"/api/appointments/{appointment.Id}",
            appointment);
    }

    [HttpGet("patient")]
    [Authorize(Policy = Policies.PatientPolicy)]
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

        var appointments =
            await _appointmentService.GetActiveAppointmentsByPatientDniAsync(
                dni,
                authenticatedEmail);

        return Ok(appointments);
    }

    [HttpDelete("{appointmentId:guid}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(
        typeof(string),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid appointmentId)
    {
        var authenticatedEmail = GetAuthenticatedEmail();

        await _appointmentService.CancelAppointmentAsync(
            appointmentId,
            authenticatedEmail);

        return Ok("ok");
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
            await _appointmentService.SearchAppointmentsAsync(request);

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
            SpecialtyId: specialtyId,
            DoctorId: doctorId,
            Dni: dni,
            Date: date,
            PageSize: pageSize,
            PageIndex: pageIndex);

        var appointments =
            await _appointmentService.SearchAppointmentsAsync(request);

        return Ok(appointments);
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