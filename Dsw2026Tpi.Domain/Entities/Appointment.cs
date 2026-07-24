namespace Dsw2026Tpi.Domain.Entities;
// TODO: Integrar Appointment con la entidad Availability cuando el módulo
// de disponibilidades sea incorporado. Reunirse con los demas para concretar la union entre los modulos.
//
// Pendiente:
// - Configurar la relación y FK Appointment.AvailabilityId -> Availability.Id.
// - Confirmar el nombre de la propiedad de fecha/hora de Availability.
// - Confirmar cómo Availability representa si el slot está disponible.
// - Garantizar que el slot corresponda al DoctorId recibido.
// - Marcar o actualizar el slot luego de reservarlo o cancelarlo.
// - Implementar las transiciones a ATTENDED y NO_SHOW cuando se desarrollen
//   las funcionalidades responsables de registrar la atención.
//
// Appointment conserva ScheduledAt como una copia de la fecha del slot para
// permitir consultas, filtros e historial sin depender siempre de Availability.
public class Appointment : EntityBase
{
    public Guid DoctorId { get; private set; }

    public Guid AvailabilityId { get; private set; }

    public Guid PatientId { get; private set; }

    public DateTime ScheduledAt { get; private set; }

    public string Reason { get; private set; }

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

        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
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