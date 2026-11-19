using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Domain.States;

// BOOKED: el único estado desde el que el turno puede cambiar.
public sealed class BookedState : AppointmentState
{
    // Singleton (GoF): el estado no guarda datos propios, así que alcanza con una instancia
    public static readonly BookedState Instance = new();

    private BookedState()
    {
    }

    public override AppointmentStatus Cancel(
        Appointment appointment) =>
        AppointmentStatus.CANCELLED;

    public override AppointmentStatus MarkAsAttended(
        Appointment appointment,
        DateTime now)
    {
        EnsureHasStarted(appointment, now);

        return AppointmentStatus.ATTENDED;
    }

    public override AppointmentStatus MarkAsNoShow(
        Appointment appointment,
        DateTime now)
    {
        EnsureHasStarted(appointment, now);

        return AppointmentStatus.NO_SHOW;
    }

    private static void EnsureHasStarted(
        Appointment appointment,
        DateTime now)
    {
        if (appointment.ScheduledAt > now)
        {
            throw InvalidTransition()
                .WithDetail(
                    "status",
                    "appointment_not_started");
        }
    }
}
