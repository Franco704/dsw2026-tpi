namespace Dsw2026Tpi.Domain.States;

public sealed class ClosedState : AppointmentState
{
    public static readonly ClosedState Instance = new();

    private ClosedState()
    {
    }
}
