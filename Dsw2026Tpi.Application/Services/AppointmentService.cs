using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Models;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Dsw2026Tpi.Application.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly IPatientAccessService _patientAccessService;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IPersistence persistence,
        IPatientAccessService patientAccessService,
        ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _patientAccessService = patientAccessService;
        _logger = logger;
    }

    public async Task<AppointmentModel.Response> CreateAsync(
        AppointmentModel.Request request,
        string authenticatedEmail)
    {
        /*
         * Este método queda pendiente hasta integrar
         * el módulo de Availability.
         */
        throw new NotImplementedException(
            "Pendiente de integrar el módulo de disponibilidades.");
    }

    public async Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetActiveByPatientDniAsync(
            long dni,
            string authenticatedEmail)
    {
        AuthenticationRequestValidator.ValidatePatientDni(dni);

        var patient = await _patientAccessService
            .GetAuthenticatedPatientAsync(authenticatedEmail);

        var requestedDni = dni.ToString(
            CultureInfo.InvariantCulture);

        if (patient.Dni != requestedDni)
        {
            throw new AuthorizationException();
        }

        var appointments =
            await _persistence.GetFiltered<Appointment>(
                appointment =>
                    appointment.PatientId == patient.Id &&
                    appointment.Status == AppointmentStatus.BOOKED &&
                    appointment.ScheduledAt >= DateTime.UtcNow);

        return appointments
            .OrderBy(appointment => appointment.ScheduledAt)
            .Select(ToResponse)
            .ToList();
    }

    public async Task CancelAsync(
        Guid appointmentId,
        string authenticatedEmail)
    {
        if (appointmentId == Guid.Empty)
        {
            var validation = new ValidationException();

            validation.WithDetail(
                "appointmentId",
                "required");

            throw validation;
        }

        var patient = await _patientAccessService
            .GetAuthenticatedPatientAsync(authenticatedEmail);

        var appointment =
            await _persistence.GetById<Appointment>(
                appointmentId);

        if (appointment is null)
        {
            throw new EntityNotFoundException(
                nameof(Appointment));
        }

        if (appointment.PatientId != patient.Id)
        {
            throw new AuthorizationException();
        }

        if (appointment.Status != AppointmentStatus.BOOKED)
        {
            throw new ConflictException(
                "APPOINTMENT_CANNOT_BE_CANCELLED",
                "Solamente se pueden cancelar turnos reservados.");
        }

        appointment.Cancel();

        await _persistence.Update(appointment);

        _logger.LogInformation(
            "Turno {AppointmentId} cancelado correctamente.",
            appointment.Id);
    }

    private static AppointmentModel.Response ToResponse(
        Appointment appointment)
    {
        return new AppointmentModel.Response(
            appointment.Id,
            appointment.DoctorId,
            appointment.AvailabilityId,
            appointment.PatientId,
            appointment.ScheduledAt,
            appointment.Reason,
            appointment.Status.ToString());
    }
}