using Dsw2026Tpi.Domain.Rules;
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

    #region Constructor for EF

    private Appointment()
    {
    }

    #endregion

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

        var normalizedReason =
            reason.Trim();

        if (normalizedReason.Length is
            < AppointmentRules.MinimumReasonLength
            or > AppointmentRules.MaximumReasonLength)
        {
            throw new ArgumentException(
                $"El motivo de la consulta debe tener entre " +
                $"{AppointmentRules.MinimumReasonLength} y " +
                $"{AppointmentRules.MaximumReasonLength} caracteres.",
                nameof(reason));
        }

        DoctorId = doctorId;
        AvailabilityId = availabilityId;
        PatientId = patientId;
        ScheduledAt = scheduledAt;
        Reason = normalizedReason;
        Status = AppointmentStatus.BOOKED;
    }

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
