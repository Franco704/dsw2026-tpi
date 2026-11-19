namespace Dsw2026Tpi.Domain.States;

// CANCELLED, ATTENDED y NO_SHOW son estados finales: no permiten ninguna transición,
// así que comparten esta clase sin sobrescribir nada.
public sealed class ClosedState : AppointmentState
{
    public static readonly ClosedState Instance = new();

    private ClosedState()
    {
    }
}
