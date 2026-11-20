using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Domain.States;

public abstract class AppointmentState
{
    public virtual AppointmentStatus Cancel(
        Appointment appointment) =>
        throw InvalidTransition();

    public virtual AppointmentStatus MarkAsAttended(
        Appointment appointment,
        DateTime now) =>
        throw InvalidTransition();

    public virtual AppointmentStatus MarkAsNoShow(
        Appointment appointment,
        DateTime now) =>
        throw InvalidTransition();

    protected static ConflictException InvalidTransition() =>
        new(
            ErrorCodes.APPOINTMENT_INVALID_STATE,
            nameof(ErrorCodes.APPOINTMENT_INVALID_STATE));
}
