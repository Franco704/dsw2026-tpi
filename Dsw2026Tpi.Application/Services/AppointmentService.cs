using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

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
    public async Task<AppointmentModel.Response> CreateAsync(
        AppointmentModel.Request request,
        string authenticatedEmail)
    {
        // Valida los datos recibidos en la request.
        AppointmentRequestValidator.ValidateCreate(
            request);

        // Obtiene el paciente asociado al usuario autenticado.
        var patient =
            await _patientAccessService
                .GetAuthenticatedPatientAsync(
                    authenticatedEmail);

        // Convierte el DNI recibido al formato almacenado.
        var requestedDni = request.Patient!.Dni.ToString(
            CultureInfo.InvariantCulture);

        // Impide crear turnos para un DNI diferente al propio.
        if (patient.Dni != requestedDni)
        {
            _logger.LogWarning(
                "El paciente {PatientId} intentó crear un turno para un DNI diferente al propio.",
                patient.Id);

            throw new AuthorizationException();
        }

        // Verifica que el médico exista y se encuentre activo.
        var doctor =
            await _persistence.GetById<Doctor>(
                request.DoctorId);

        if (doctor is null ||
            doctor.Deleted ||
            !doctor.IsActive)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        // Obtiene la disponibilidad seleccionada.
        var availability =
            await _persistence.GetById<Availability>(
                request.AvailabilityId);

        if (availability is null)
        {
            throw new EntityNotFoundException(
                nameof(Availability));
        }

        // Verifica que la disponibilidad pertenezca al médico.
        if (availability.DoctorId != doctor.Id)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }

        // Verifica que el bloque todavía esté disponible.
        if (!availability.IsAvailable)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }

        // Construye la fecha y hora real del turno.
        var scheduledAt = availability.Date.Date
            .Add(availability.StartTime);

        // Impide reservar turnos en fechas pasadas.
        if (scheduledAt <= DateTime.Now)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_PAST_DATE,
                nameof(ErrorCodes.APPOINTMENT_PAST_DATE));
        }

        /*
         * Verifica que no exista otro turno reservado
         * para la misma disponibilidad.
         */
        var existingAppointment =
            await _persistence.First<Appointment>(
                appointment =>
                    appointment.AvailabilityId ==
                    availability.Id
                    &&
                    appointment.Status ==
                    AppointmentStatus.BOOKED);

        if (existingAppointment is not null)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }

        // Crea la entidad de dominio con los datos resueltos.
        var appointment = new Appointment(
            doctor.Id,
            availability.Id,
            patient.Id,
            scheduledAt,
            request.Reason!);

        // Marca el bloque como ocupado.
        availability.MarkAsUnavailable();

        
        //Control de Concurrencia
        /* Si el índice único filtrado en BD salta por
         dos peticiones simultáneas atrapamos DbUpdateException*/

        try
        {
            await _persistence.Add(appointment);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex,
                "Conflicto de concurrencia, turno {AvailabilityID} ya fue reservado",
                availability.Id);
            
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_CONFLICT,
                nameof(ErrorCodes.APPOINTMENT_CONFLICT));
        }
        _logger.LogInformation(
            "Reserva confirmada. Turno {AppointmentId}, paciente {PatientId}, disponibilidad {AvailabilityId}.",
            appointment.Id,
            patient.Id,
            availability.Id);
        
        /*
         * Appointment se agrega y Availability ya está trackeada.
         * SaveChangesAsync persiste ambas modificaciones.
         */
        
        _logger.LogInformation(
            "Reserva confirmada. Turno {AppointmentId}, paciente {PatientId}, disponibilidad {AvailabilityId}.",
            appointment.Id,
            patient.Id,
            availability.Id);

        return ToResponse(
            appointment);
    }

    /// <summary>
    /// Obtiene los turnos activos del paciente autenticado,
    /// verificando que el DNI solicitado sea el propio.
    /// </summary>
    public async Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetActiveByPatientDniAsync(
            long dni,
            string authenticatedEmail)
    {
        // Valida el formato del DNI recibido.
        AuthenticationRequestValidator.ValidatePatientDni(
            dni);

        // Obtiene el paciente asociado al usuario autenticado.
        var patient =
            await _patientAccessService
                .GetAuthenticatedPatientAsync(
                    authenticatedEmail);

        var requestedDni = dni.ToString(
            CultureInfo.InvariantCulture);

        // Impide consultar los turnos de otro paciente.
        if (patient.Dni != requestedDni)
        {
            throw new AuthorizationException();
        }

        // Obtiene únicamente turnos reservados y futuros.
        var appointments =
            await _persistence.GetFiltered<Appointment>(
                appointment =>
                    appointment.PatientId == patient.Id
                    &&
                    appointment.Status ==
                    AppointmentStatus.BOOKED
                    &&
                    appointment.ScheduledAt >= DateTime.Now);

        return appointments
            .OrderBy(
                appointment =>
                    appointment.ScheduledAt)
            .Select(ToResponse)
            .ToList();
    }

    /// <summary>
    /// Cancela un turno perteneciente al paciente autenticado
    /// y libera la disponibilidad asociada.
    /// </summary>
    public async Task CancelAsync(
        Guid appointmentId,
        string authenticatedEmail)
    {
        // Valida que el identificador del turno sea válido.
        if (appointmentId == Guid.Empty)
        {
            var validation =
                new ValidationException();

            validation.WithDetail(
                nameof(appointmentId),
                "El identificador del turno es obligatorio.");

            throw validation;
        }

        // Obtiene el paciente asociado al usuario autenticado.
        var patient =
            await _patientAccessService
                .GetAuthenticatedPatientAsync(
                    authenticatedEmail);

        // Obtiene el turno que se desea cancelar.
        var appointment =
            await _persistence.GetById<Appointment>(
                appointmentId);

        if (appointment is null)
        {
            throw new EntityNotFoundException(
                nameof(Appointment));
        }

        // Impide cancelar turnos pertenecientes a otro paciente.
        if (appointment.PatientId != patient.Id)
        {
            throw new AuthorizationException();
        }

        // Solo los turnos reservados pueden cancelarse.
        if (appointment.Status != AppointmentStatus.BOOKED)
        {
            throw new ConflictException(
                ErrorCodes.APPOINTMENT_INVALID_STATE,
                nameof(ErrorCodes.APPOINTMENT_INVALID_STATE));
        }

        // Obtiene la disponibilidad asociada al turno.
        var availability =
            await _persistence.GetById<Availability>(
                appointment.AvailabilityId);

        if (availability is null)
        {
            throw new EntityNotFoundException(
                nameof(Availability));
        }

        // Cambia el estado de ambas entidades.
        appointment.Cancel();
        availability.MarkAsAvailable();

        /*
         * Appointment y Availability fueron obtenidas desde
         * el mismo DbContext, por lo tanto ambas están trackeadas.
         *
         * Update ejecuta SaveChangesAsync y persiste:
         * Appointment.Status = CANCELLED
         * Availability.IsAvailable = true
         */
        await _persistence.Update(
            appointment);

        _logger.LogInformation(
            "Turno {AppointmentId} cancelado correctamente y disponibilidad {AvailabilityId} liberada.",
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
            ToSearchResponse);
    }

    /// <summary>
    /// Convierte un turno en el DTO utilizado por la búsqueda.
    /// </summary>
    private static AppointmentModel.SearchResponse ToSearchResponse(
        Appointment appointment)
    {
        return new AppointmentModel.SearchResponse(
            appointment.Id,
            appointment.Doctor.SpecialityId,
            appointment.Doctor.Speciality?.Name ??
            string.Empty,
            appointment.DoctorId,
            appointment.Doctor.Name,
            appointment.ScheduledAt,
            appointment.Status.ToString());
    }

    /// <summary>
    /// Convierte un turno en el DTO general de respuesta.
    /// </summary>
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

/*
 * DECISIONES TOMADAS:
 *
 * - Se mantuvo la estructura original de AppointmentService.
 *
 * - No se extrajeron nuevos servicios ni componentes.
 *
 * - AppointmentRequestValidator continúa validando
 *   los contratos de creación y búsqueda.
 *
 * - AuthorizationException se utiliza cuando el paciente
 *   intenta operar sobre datos que no le pertenecen.
 *
 * - EntityNotFoundException se utiliza para Doctor,
 *   Availability y Appointment inexistentes.
 *
 * - APPOINTMENT_CONFLICT se utiliza cuando la disponibilidad
 *   no corresponde, no está libre o ya posee un turno reservado.
 *
 * - APPOINTMENT_PAST_DATE se utiliza cuando la fecha calculada
 *   del turno ya pasó.
 *
 * - APPOINTMENT_INVALID_STATE se utiliza cuando se intenta
 *   cancelar un turno que no está en estado BOOKED.
 *
 * - ConflictException recibe primero el mensaje
 *   y luego el código de error.
 *
 * - Se utiliza DateTime.Now para mantener el criterio temporal
 *   definido en el proyecto.
 *
 * - Los cambios de estado se realizan mediante métodos
 *   de dominio como Cancel, MarkAsAvailable y MarkAsUnavailable.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - En CreateAsync, Appointment y Availability se modifican en
 *   una misma operación, pero el diseño depende del tracking
 *   automático del mismo DbContext.
 *
 * - Si Add utilizara un contexto distinto o AsNoTracking,
 *   el cambio de Availability podría no persistirse.
 *
 * - La verificación de disponibilidad y la inserción del turno
 *   no son atómicas frente a solicitudes concurrentes.
 *
 * - Dos solicitudes simultáneas podrían superar las validaciones
 *   antes de que una de ellas confirme SaveChangesAsync.
 *
 * - La base debe conservar un índice único o filtrado sobre
 *   AvailabilityId para turnos activos.
 *
 * - Sería conveniente traducir una excepción de concurrencia
 *   o de índice único a APPOINTMENT_CONFLICT.
 *
 * - CancelAsync depende de que Appointment y Availability estén
 *   trackeadas dentro del mismo DbContext.
 *
 * - Sería más explícito disponer de SaveChangesAsync en
 *   IPersistence para confirmar modificaciones de varias entidades.
 *
 * - SearchAsync utiliza navegaciones de Doctor, Speciality,
 *   Availability y Patient. Debe verificarse que los nombres
 *   utilizados en Include coincidan con las propiedades reales.
 *
 * - Appointment almacena DoctorId y ScheduledAt aunque ambos
 *   pueden derivarse de Availability. Debe mantenerse una regla
 *   de consistencia entre esos valores.
 *
 * - La validación Guid.Empty de CancelAsync podría trasladarse
 *   a un validador específico, pero se mantiene en el método
 *   para respetar la estructura original.
 *
 * - Los logs registran identificadores internos, pero no incluyen
 *   DNI, motivo del turno ni token, evitando datos sensibles.
 */