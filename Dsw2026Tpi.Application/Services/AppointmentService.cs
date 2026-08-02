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

/// <summary>
/// Gestiona los casos de uso relacionados con turnos,
/// incluyendo reserva, consulta, cancelación y búsqueda.
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly IPersistence _persistence;
    private readonly IPatientAccessService _patientAccessService;
    private readonly ILogger<AppointmentService> _logger;

    /// <summary>
    /// Inicializa el servicio con las abstracciones necesarias
    /// para persistir turnos, resolver pacientes y registrar eventos.
    /// </summary>
    public AppointmentService(
        IPersistence persistence,
        IPatientAccessService patientAccessService,
        ILogger<AppointmentService> logger)
    {
        _persistence = persistence;
        _patientAccessService = patientAccessService;
        _logger = logger;
    }

    /// <summary>
    /// Reserva un turno para el paciente autenticado.
    /// </summary>
    /// <summary>
    /// Reserva un turno para el paciente autenticado.
    /// </summary>
    public async Task<AppointmentModel.Response> CreateAsync(
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

        /*
         * Appointment se agrega y Availability ya está trackeada.
         * SaveChangesAsync persiste ambas modificaciones.
         */
        try
        {
            await _persistence.Add(appointment);
        }
        catch (DbUpdateException ex)
        {
            // Otro request reservó el mismo slot al mismo tiempo, el índice único lo frenó en config.
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
    /// <summary>
    /// Obtiene los turnos activos del paciente autenticado,
    /// verificando que el DNI solicitado sea el propio.
    /// </summary>
    /// <summary>
    /// Obtiene los turnos activos del paciente autenticado,
    /// verificando que el DNI solicitado sea el propio.
    /// </summary>
    public async Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetActiveByPatientDniAsync(
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
    /// <summary>
    /// Cancela un turno perteneciente al paciente autenticado
    /// y libera la disponibilidad asociada.
    /// </summary>
    /// <summary>
    /// Cancela un turno perteneciente al paciente autenticado
    /// y libera la disponibilidad asociada.
    /// </summary>
    public async Task CancelAsync(
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

        /*
         * Ambas entidades fueron obtenidas desde el mismo DbContext.
         * Update confirma conjuntamente los cambios pendientes.
         */
        await _persistence.Update(
            appointment);

        _logger.LogInformation(
            "Turno {AppointmentId} cancelado correctamente " +
            "y disponibilidad {AvailabilityId} liberada.",
            appointment.Id,
            availability.Id);
    }

    /// <summary>
    /// Busca turnos utilizando filtros opcionales
    /// y devuelve los resultados paginados.
    /// </summary>
    public async Task<Pagination<AppointmentModel.SearchResponse>>
        SearchAsync(
            AppointmentModel.SearchRequest request)
    {
        // Valida los filtros y parámetros de paginación.
        AppointmentRequestValidator.ValidateSearch(
            request);

        var dni = request.Dni?.ToString(
            CultureInfo.InvariantCulture);

        var dateFrom =
            request.Date?.Date;

        var dateTo =
            dateFrom?.AddDays(1);

        /*
         * Aplica únicamente los filtros que fueron informados
         * y carga las navegaciones necesarias para la respuesta.
         */
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

    /// <summary>
    /// Comprueba que el DNI solicitado corresponda
    /// al paciente autenticado.
    /// </summary>
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

    /// <summary>
    /// Obtiene un médico activo o informa que no existe.
    /// </summary>
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

    /// <summary>
    /// Obtiene una disponibilidad o informa que no existe.
    /// </summary>
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

    /// <summary>
    /// Comprueba que la disponibilidad seleccionada
    /// pertenezca al médico solicitado.
    /// </summary>
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

    /// <summary>
    /// Comprueba que el bloque pueda reservarse y devuelve
    /// la fecha y hora concreta del turno.
    /// </summary>
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

    /// <summary>
    /// Comprueba que no exista otro turno reservado
    /// para la misma disponibilidad.
    /// </summary>
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

    /// <summary>
    /// Obtiene un turno mediante su identificador
    /// o informa que no existe.
    /// </summary>
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

    /// <summary>
    /// Comprueba que el turno pertenezca
    /// al paciente autenticado.
    /// </summary>
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

    /// <summary>
    /// Comprueba que el turno todavía pueda cancelarse.
    /// </summary>
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