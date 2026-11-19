namespace Dsw2026Tpi.Domain.Entities;

public class AvailabilityRule : DeletedEntity
{
    public Guid DoctorId { get; private set; }

    public int Year { get; private set; }

    public int Month { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeSpan StartTime { get; private set; }

    public TimeSpan EndTime { get; private set; }

    #region Constructor for EF

    private AvailabilityRule()
    {
    }

    #endregion

    public AvailabilityRule(
        Guid doctorId,
        int year,
        int month,
        DayOfWeek dayOfWeek,
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

        if (month is < 1 or > 12)
        {
            throw new ArgumentException(
                "El mes debe estar entre 1 y 12.",
                nameof(month));
        }

        if (endTime <= startTime)
        {
            throw new ArgumentException(
                "La hora de finalización debe ser posterior a la hora de inicio.",
                nameof(endTime));
        }

        DoctorId = doctorId;
        Year = year;
        Month = month;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }
}
