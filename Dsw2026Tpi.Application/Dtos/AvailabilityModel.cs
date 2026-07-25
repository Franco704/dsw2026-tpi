namespace Dsw2026Tpi.Application.Dtos;

public record AvailabilityModel
{
    public record Request(Guid DoctorId, List<DaySchedule> Days);
    public record Response(Guid Id, Guid DoctorId, DateTime Date, TimeSpan StartTime, TimeSpan EndTime);

    public class DaySchedule
    {
        public string Day { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}