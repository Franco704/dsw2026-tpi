namespace Dsw2026Tpi.Application.Dtos;

public record DashboardModel
{
    public record StatsResponse(
        int ActiveDoctors,
        int ActiveSpecialties,
        int ConfirmedAppointmentsToday);
}
