using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
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
        AppointmentRequestValidator.ValidateCreate(request);

        var patient = await _patientAccessService
            .GetAuthenticatedPatientAsync(authenticatedEmail);

        var requestedDni = request.Patient!.Dni.ToString(
            CultureInfo.InvariantCulture);

        if (patient.Dni != requestedDni)
        {
            _logger.LogWarning(
                "El paciente {PatientId} intentó crear un turno para un DNI diferente al propio.",
                patient.Id);

            throw new AuthorizationException();
        }

        var doctor = await _persistence.GetById<Doctor>(
            request.DoctorId);

        if (doctor is null || doctor.Deleted || !doctor.IsActive)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        var availability =
            await _persistence.GetById<Availability>(
                request.AvailabilityId);

        if (availability is null)
        {
            throw new EntityNotFoundException(
                nameof(Availability));
        }

        if (availability.DoctorId != doctor.Id)
        {
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_CONFLICT),
                ErrorCodes.APPOINTMENT_CONFLICT);
        }

        if (!availability.IsAvailable)
        {
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_CONFLICT),
                ErrorCodes.APPOINTMENT_CONFLICT);
        }

        var scheduledAt = availability.Date.Date
            .Add(availability.StartTime);

        if (scheduledAt <= DateTime.Now)
        {
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_PAST_DATE),
                ErrorCodes.APPOINTMENT_PAST_DATE);
        }

        var existingAppointment =
            await _persistence.First<Appointment>(
                appointment =>
                    appointment.AvailabilityId == availability.Id &&
                    appointment.Status == AppointmentStatus.BOOKED);

        if (existingAppointment is not null)
        {
            throw new ConflictException(
                nameof(ErrorCodes.APPOINTMENT_CONFLICT),
                ErrorCodes.APPOINTMENT_CONFLICT);
        }

        var appointment = new Appointment(
            doctor.Id,
            availability.Id,
            patient.Id,
            scheduledAt,
            request.Reason!);

        availability.MarkAsUnavailable();

        await _persistence.Add(appointment);

        _logger.LogInformation(
     "Reserva confirmada. Turno {AppointmentId}, paciente {PatientId}, disponibilidad {AvailabilityId}.",
     appointment.Id,
     patient.Id,
     availability.Id);
        return ToResponse(appointment);
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
                nameof(ErrorCodes.APPOINTMENT_INVALID_STATE),
                ErrorCodes.APPOINTMENT_INVALID_STATE);
        }

        var availability =
            await _persistence.GetById<Availability>(
                appointment.AvailabilityId);

        if (availability is null)
        {
            throw new EntityNotFoundException(
                nameof(Availability));
        }

        appointment.Cancel();
        availability.MarkAsAvailable();

        /*
         * Appointment y Availability fueron obtenidas desde
         * el mismo DbContext, por lo tanto ambas están trackeadas.
         *
         * Update ejecuta un único SaveChangesAsync y persiste:
         * Appointment.Status = CANCELLED
         * Availability.IsAvailable = true
         */
        await _persistence.Update(appointment);

        _logger.LogInformation(
            "Turno {AppointmentId} cancelado correctamente y disponibilidad {AvailabilityId} liberada.",
            appointment.Id,
            availability.Id);
    }

    public async Task<Pagination<AppointmentModel.SearchResponse>>
        SearchAsync(
            AppointmentModel.SearchRequest request)
    {
        AppointmentRequestValidator.ValidateSearch(request);

        var dni = request.Dni?.ToString(
            CultureInfo.InvariantCulture);

        var dateFrom = request.Date?.Date;
        var dateTo = dateFrom?.AddDays(1);

        var appointments =
            await _persistence.Paginate<Appointment, DateTime>(
                request.PageSize,
                request.PageIndex,
                appointment =>
                    (!request.SpecialtyId.HasValue ||
                        appointment.Doctor.SpecialityId ==
                        request.SpecialtyId.Value)
                    &&
                    (!request.DoctorId.HasValue ||
                        appointment.DoctorId ==
                        request.DoctorId.Value)
                    &&
                    (dni == null ||
                        appointment.Patient.Dni == dni)
                    &&
                    (!dateFrom.HasValue ||
                        appointment.ScheduledAt >= dateFrom.Value &&
                        appointment.ScheduledAt < dateTo!.Value),
                appointment => appointment.ScheduledAt,
                "Doctor",
                "Doctor.Speciality",
                "Availability",
                "Patient");

        return appointments.Map(ToSearchResponse);
    }

    private static AppointmentModel.SearchResponse ToSearchResponse(
        Appointment appointment)
    {
        return new AppointmentModel.SearchResponse(
            appointment.Id,
            appointment.Doctor.SpecialityId,
            appointment.Doctor.Speciality?.Name ?? string.Empty,
            appointment.DoctorId,
            appointment.Doctor.Name,
            appointment.ScheduledAt,
            appointment.Status.ToString());
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