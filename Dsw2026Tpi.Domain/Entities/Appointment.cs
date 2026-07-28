namespace Dsw2026Tpi.Domain.Entities;

public class Appointment : EntityBase
{
    public Guid DoctorId { get; private set; }
    public Doctor Doctor { get; private set; } = null!;

    public Guid AvailabilityId { get; private set; }
    public Availability Availability { get; private set; } = null!;

    public Guid PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;

    public DateTime ScheduledAt { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    

    public AppointmentStatus Status { get; private set; }
    private Appointment()
    {
    }


    public Appointment(
        Guid doctorId,
        Guid availabilityId,
        Guid patientId,
        DateTime scheduledAt,
        string reason,
        Guid? id = null) : base(id)
    {
        DoctorId = doctorId;
        AvailabilityId = availabilityId;
        PatientId = patientId;
        ScheduledAt = scheduledAt;
        Reason = reason.Trim();
        Status = AppointmentStatus.BOOKED;

        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }

    public void Cancel()
    {
        if (Status != AppointmentStatus.BOOKED)
        {
            throw new InvalidOperationException(
                "Only booked appointments can be cancelled.");
        }

        Status = AppointmentStatus.CANCELLED;
        UpdatedAt = DateTime.UtcNow;
    }
}