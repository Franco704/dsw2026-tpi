using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Dsw2026Tpi.Application.Mappers;
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
    //GetActiveAppointmentsByPatientDniAsync
    //CancelAppointmentAsync
    // SearchAppointmentsAsync

    public async Task<AppointmentModel.Response> CreateAppointmentAsync(
        AppointmentModel.Request request,
        string authenticatedEmail)
    {
        AppointmentRequestValidator.ValidateCreate(
            request);

        var patient =
            await _patientAccessService
                .GetAuthenticatedPatientAsync(
                    authenticatedEmail);

        EnsurePatientDniMatches(
            patient,
            request.Patient!.Dni);

        var doctor =
            await GetRequiredDoctorAsync(
                request.DoctorId);

        var availability =
            await GetRequiredAvailabilityAsync(
                request.AvailabilitySlotId);

        EnsureAvailabilityBelongsToDoctor(
            availability,
            doctor.Id);

        var scheduledAt =
            GetBookableScheduledAt(
                availability);

        await EnsureNoBookedAppointmentExistsAsync(
            availability.Id);

        var appointment =
            new Appointment(
                doctor.Id,
                availability.Id,
                patient.Id,
                scheduledAt,
                request.Reason!);

        availability.MarkAsUnavailable();

        try
        {
            await _persistence.Add(appointment);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex,
                "Reserva concurrente rechazada por el índice único. Disponibilidad {AvailabilityId}, paciente {PatientId}.",
                availability.Id,
                patient.Id);

            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }

        _logger.LogInformation(
            "Reserva confirmada. Turno {AppointmentId}, " +
            "paciente {PatientId}, disponibilidad {AvailabilityId}.",
            appointment.Id,
            patient.Id,
            availability.Id);

        return AppointmentMapper.ToResponse(
            appointment);
    }
    public async Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetActiveAppointmentsByPatientDniAsync(
            long dni,
            string authenticatedEmail)
    {
        AuthenticationRequestValidator.ValidatePatientDni(
            dni);

        var patient =
            await _patientAccessService
                .GetAuthenticatedPatientAsync(
                    authenticatedEmail);

        EnsurePatientDniMatches(
            patient,
            dni);

        var appointments =
            await _persistence.GetFiltered<Appointment>(
                appointment =>
                    appointment.PatientId == patient.Id &&
                    appointment.Status == AppointmentStatus.BOOKED &&
                    appointment.ScheduledAt >= DateTime.Now);

        return (appointments ?? [])
            .OrderBy(
                appointment =>
                    appointment.ScheduledAt)
            .Select(
                AppointmentMapper.ToResponse)
            .ToList();
    }
    public async Task CancelAppointmentAsync(
        Guid appointmentId,
        string authenticatedEmail)
    {
        AppointmentRequestValidator.ValidateCancel(
            appointmentId);

        var patient =
            await _patientAccessService
                .GetAuthenticatedPatientAsync(
                    authenticatedEmail);

        var appointment =
            await GetRequiredAppointmentAsync(
                appointmentId);

        EnsureAppointmentBelongsToPatient(
            appointment,
            patient);

        EnsureAppointmentCanBeCancelled(
            appointment);

        var availability =
            await GetRequiredAvailabilityAsync(
                appointment.AvailabilityId);

        appointment.Cancel();
        availability.MarkAsAvailable();

        await _persistence.Update(
            appointment);

        _logger.LogInformation(
            "Turno {AppointmentId} cancelado correctamente " +
            "y disponibilidad {AvailabilityId} liberada.",
            appointment.Id,
            availability.Id);
    }

    public async Task<Pagination<AppointmentModel.SearchResponse>>
        SearchAppointmentsAsync(
            AppointmentModel.SearchRequest request)
    {
        AppointmentRequestValidator.ValidateSearch(
            request);

        var dni = request.Dni?.ToString(
            CultureInfo.InvariantCulture);

        var dateFrom =
            request.Date?.Date;

        var dateTo =
            dateFrom?.AddDays(1);

        var appointments =
            await _persistence.Paginate<Appointment, DateTime>(
                request.PageSize,
                request.PageIndex,
                appointment =>
                    (
                        !request.SpecialtyId.HasValue
                        ||
                        appointment.Doctor.SpecialityId ==
                        request.SpecialtyId.Value
                    )
                    &&
                    (
                        !request.DoctorId.HasValue
                        ||
                        appointment.DoctorId ==
                        request.DoctorId.Value
                    )
                    &&
                    (
                        dni == null
                        ||
                        appointment.Patient.Dni == dni
                    )
                    &&
                    (
                        !dateFrom.HasValue
                        ||
                        appointment.ScheduledAt >= dateFrom.Value
                        &&
                        appointment.ScheduledAt < dateTo!.Value
                    ),
                appointment =>
                    appointment.ScheduledAt,
                nameof(Appointment.Doctor),
                $"{nameof(Appointment.Doctor)}.{nameof(Doctor.Speciality)}",
                nameof(Appointment.Availability),
                nameof(Appointment.Patient));

        return appointments.Map(
            AppointmentMapper.ToSearchResponse);
    }

    private void EnsurePatientDniMatches(
        Patient patient,
        long requestedDni)
    {
        var requestedDniText =
            requestedDni.ToString(
                CultureInfo.InvariantCulture);

        if (patient.Dni == requestedDniText)
        {
            return;
        }

        _logger.LogWarning(
            "El paciente {PatientId} intentó operar con un DNI diferente al propio.",
            patient.Id);

        throw new AuthorizationException();
    }

    private async Task<Doctor> GetRequiredDoctorAsync(
        Guid doctorId)
    {
        var doctor =
            await _persistence.GetById<Doctor>(
                doctorId);

        if (doctor is null ||
            doctor.Deleted ||
            !doctor.IsActive)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        return doctor;
    }

    private async Task<Availability> GetRequiredAvailabilityAsync(
        Guid availabilityId)
    {
        var availability =
            await _persistence.GetById<Availability>(
                availabilityId);

        if (availability is null)
        {
            throw new EntityNotFoundException(
                nameof(Availability));
        }

        return availability;
    }

    private static void EnsureAvailabilityBelongsToDoctor(
        Availability availability,
        Guid doctorId)
    {
        if (availability.DoctorId == doctorId)
        {
            return;
        }

        throw new ConflictException(
            ErrorCodes.APPOINTMENT_CONFLICT,
            nameof(ErrorCodes.APPOINTMENT_CONFLICT));
    }

    private static DateTime GetBookableScheduledAt(
        Availability availability)
    {
        if (!availability.IsAvailable)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }

        var scheduledAt =
            availability.Date.Date.Add(
                availability.StartTime);

        if (scheduledAt <= DateTime.Now)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_PAST_DATE,
                nameof(ErrorCodes.APPOINTMENT_PAST_DATE));
        }

        return scheduledAt;
    }

    private async Task EnsureNoBookedAppointmentExistsAsync(
        Guid availabilityId)
    {
        var existingAppointment =
            await _persistence.First<Appointment>(
                appointment =>
                    appointment.AvailabilityId == availabilityId &&
                    appointment.Status == AppointmentStatus.BOOKED);

        if (existingAppointment is not null)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }
    }

    private async Task<Appointment> GetRequiredAppointmentAsync(
        Guid appointmentId)
    {
        var appointment =
            await _persistence.GetById<Appointment>(
                appointmentId);

        if (appointment is null)
        {
            throw new EntityNotFoundException(
                nameof(Appointment));
        }

        return appointment;
    }

    private static void EnsureAppointmentBelongsToPatient(
        Appointment appointment,
        Patient patient)
    {
        if (appointment.PatientId == patient.Id)
        {
            return;
        }

        throw new AuthorizationException();
    }

    private static void EnsureAppointmentCanBeCancelled(
        Appointment appointment)
    {
        if (appointment.Status == AppointmentStatus.BOOKED)
        {
            return;
        }

        throw new ConflictException(
            ErrorCodes.APPOINTMENT_INVALID_STATE,
            nameof(ErrorCodes.APPOINTMENT_INVALID_STATE));
    }

}
