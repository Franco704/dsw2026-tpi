namespace Dsw2026Tpi.Domain.Entities;

public class Availability : DeletedEntity
{
    public Guid DoctorId { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    public DateTime Date { get; private set; }

    public TimeSpan StartTime { get; private set; }

    public TimeSpan EndTime { get; private set; }

    public bool IsAvailable { get; private set; }
    
    public DateTime StartsAt => Date.Add(StartTime);

    public Guid? AvailabilityRuleId { get; private set; }

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
        Guid? id = null,
        Guid? availabilityRuleId = null)
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
        AvailabilityRuleId = availabilityRuleId;
    }

    public bool OverlapsWith(Availability other) =>
        DoctorId == other.DoctorId &&
        Date == other.Date &&
        StartTime < other.EndTime &&
        EndTime > other.StartTime;

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

