namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Representa un bloque horario disponible de un médico.
/// </summary>
public class Availability : EntityBase
{
    /// <summary>
    /// Identificador del médico propietario de la disponibilidad.
    /// </summary>
    public Guid DoctorId { get; private set; }

    /// <summary>
    /// Médico asociado a la disponibilidad.
    /// </summary>
    public Doctor Doctor { get; private set; } = null!;

    /// <summary>
    /// Fecha correspondiente al bloque horario.
    /// </summary>
    public DateTime Date { get; private set; }

    /// <summary>
    /// Hora de inicio del bloque.
    /// </summary>
    public TimeSpan StartTime { get; private set; }

    /// <summary>
    /// Hora de finalización del bloque.
    /// </summary>
    public TimeSpan EndTime { get; private set; }

    /// <summary>
    /// Indica si el bloque se encuentra disponible para reservar.
    /// </summary>
    public bool IsAvailable { get; private set; }

    #region Constructor for EF

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    private Availability()
    {
    }

    #endregion

    /// <summary>
    /// Crea una nueva disponibilidad para un médico.
    /// </summary>
    public Availability(
        Guid doctorId,
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime,
        Guid? id = null)
        : base(id)
    {
        if (doctorId == Guid.Empty)
        {
            throw new ArgumentException(
                "El médico es obligatorio.",
                nameof(doctorId));
        }

        if (date.Date < DateTime.Today)
        {
            throw new ArgumentException(
                "La fecha de disponibilidad no puede ser pasada.",
                nameof(date));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException(
                "La hora de finalización debe ser posterior a la hora de inicio.",
                nameof(endTime));
        }

        DoctorId = doctorId;
        Date = date.Date;
        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = true;
    }

    /// <summary>
    /// Marca el bloque horario como no disponible.
    /// </summary>
    public void MarkAsUnavailable()
    {
        if (!IsAvailable)
        {
            return;
        }

        IsAvailable = false;
        MarkAsUpdated();
    }

    /// <summary>
    /// Marca nuevamente el bloque horario como disponible.
    /// </summary>
    public void MarkAsAvailable()
    {
        if (IsAvailable)
        {
            return;
        }

        IsAvailable = true;
        MarkAsUpdated();
    }
}

/*
 * CONSIDERACIONES:
 *
 * - CreatedAt y UpdatedAt se administran desde EntityBase.
 * - La superposición con otras disponibilidades debe validarse
 *   en Application porque requiere consultar la persistencia.
 * - IsAvailable solo representa disponible o no disponible;
 *   no diferencia entre reservado y bloqueado.
 */