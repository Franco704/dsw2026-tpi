namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Representa un turno reservado por un paciente
/// para una disponibilidad médica determinada.
/// </summary>
public class Appointment : EntityBase
{
    /// <summary>
    /// Identificador del médico asociado al turno.
    /// </summary>
    public Guid DoctorId { get; private set; }

    /// <summary>
    /// Médico asociado al turno.
    /// </summary>
    public Doctor Doctor { get; private set; } = null!;

    /// <summary>
    /// Identificador de la disponibilidad reservada.
    /// </summary>
    public Guid AvailabilityId { get; private set; }

    /// <summary>
    /// Disponibilidad utilizada para reservar el turno.
    /// </summary>
    public Availability Availability { get; private set; } = null!;

    /// <summary>
    /// Identificador del paciente propietario del turno.
    /// </summary>
    public Guid PatientId { get; private set; }

    /// <summary>
    /// Paciente propietario del turno.
    /// </summary>
    public Patient Patient { get; private set; } = null!;

    /// <summary>
    /// Fecha y hora programada del turno.
    /// </summary>
    public DateTime ScheduledAt { get; private set; }

    /// <summary>
    /// Motivo informado para la consulta.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Estado actual del turno.
    /// </summary>
    public AppointmentStatus Status { get; private set; }

    #region Constructor for EF

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    private Appointment()
    {
    }

    #endregion

    /// <summary>
    /// Crea un nuevo turno en estado reservado.
    /// </summary>
    public Appointment(
        Guid doctorId,
        Guid availabilityId,
        Guid patientId,
        DateTime scheduledAt,
        string reason,
        Guid? id = null)
        : base(id)
    {
        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El médico es obligatorio.",
                nameof(doctorId));
        }

        if (availabilityId == Guid.Empty)
        {
            throw new ArgumentException(
                "La disponibilidad es obligatoria.",
                nameof(availabilityId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException(
                "El paciente es obligatorio.",
                nameof(patientId));
        }

        if (scheduledAt <= DateTime.Now)
        {
            throw new ArgumentException(
                "La fecha del turno debe ser futura.",
                nameof(scheduledAt));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "El motivo de la consulta es obligatorio.",
                nameof(reason));
        }

        DoctorId = doctorId;
        AvailabilityId = availabilityId;
        PatientId = patientId;
        ScheduledAt = scheduledAt;
        Reason = reason.Trim();
        Status = AppointmentStatus.BOOKED;
    }

    /// <summary>
    /// Cancela un turno que se encuentra reservado.
    /// </summary>
    public void Cancel()
    {
        if (Status != AppointmentStatus.BOOKED)
        {
            throw new InvalidOperationException(
                "Solo puede cancelarse un turno reservado.");
        }

        Status = AppointmentStatus.CANCELLED;
        MarkAsUpdated();
    }
}

/*
 * CONSIDERACIONES:
 *
 * - Id, CreatedAt, UpdatedAt y Deleted se administran desde EntityBase.
 * - DoctorId y ScheduledAt duplican información accesible desde Availability.
 * - Application debe verificar que DoctorId, ScheduledAt y Availability
 *   correspondan entre sí antes de crear el turno.
 * - Cancel() modifica únicamente el turno; Application debe liberar
 *   la disponibilidad y persistir ambos cambios.
 * - El modelo físico contempla CancelledAt, pero la entidad actual
 *   todavía no registra la fecha específica de cancelación.
 */