using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IPersistence _persistence;

    public DashboardService(
        IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<DashboardModel.StatsResponse> GetStatsAsync(
        DateTime? date)
    {
        if (!date.HasValue)
        {
            throw new ValidationException()
                .WithDetail(
                    "date",
                    "required");
        }

        var dayStart =
            date.Value.Date;

        var nextDayStart =
            dayStart.AddDays(1);

        var activeDoctors =
            await _persistence.Count<Doctor>(
                doctor =>
                    doctor.IsActive &&
                    !doctor.Deleted);

        var activeSpecialties =
            await _persistence.Count<Specialty>(
                specialty =>
                    specialty.IsActive &&
                    !specialty.Deleted);

        var confirmedAppointmentsToday =
            await _persistence.Count<Appointment>(
                appointment =>
                    appointment.Status == AppointmentStatus.BOOKED &&
                    appointment.ScheduledAt >= dayStart &&
                    appointment.ScheduledAt < nextDayStart);

        return new DashboardModel.StatsResponse(
            ActiveDoctors:
                activeDoctors,

            ActiveSpecialties:
                activeSpecialties,

            ConfirmedAppointmentsToday:
                confirmedAppointmentsToday);
    }
}
