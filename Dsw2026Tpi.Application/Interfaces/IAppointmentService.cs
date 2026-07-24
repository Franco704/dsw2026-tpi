using Dsw2026Tpi.Application.Models;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> CreateAsync(
        AppointmentModel.Request request,
        string authenticatedEmail);

    Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetActiveByPatientDniAsync(
            long dni,
            string authenticatedEmail);

    Task CancelAsync(
        Guid appointmentId,
        string authenticatedEmail);
}