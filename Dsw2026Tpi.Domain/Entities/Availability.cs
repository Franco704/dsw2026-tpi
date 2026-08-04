namespace Dsw2026Tpi.Domain.Entities;

public class Availability : DeletedEntity
{
    public Guid DoctorId { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    public DateTime Date { get; private set; }

    public TimeSpan StartTime { get; private set; }

    public TimeSpan EndTime { get; private set; }

    public bool IsAvailable { get; private set; }

    #region Constructor for EF

    private Availability()
    {
    }

    #endregion

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

    public void MarkAsUnavailable()
    {
        if (!IsAvailable)
        {
            return;
        }

        IsAvailable = false;
        MarkAsUpdated();
    }

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
