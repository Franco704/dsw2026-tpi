using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentModel.Response> CreateAppointmentAsync(
        AppointmentModel.Request request,
        string authenticatedEmail);

    Task<IReadOnlyCollection<AppointmentModel.Response>>
        GetActiveByPatientDniAsync(
            long dni,
            string authenticatedEmail);

    Task CancelAsync(
        Guid appointmentId,
        string authenticatedEmail);

    Task<Pagination<AppointmentModel.SearchResponse>> SearchAppointmentAsync(
    AppointmentModel.SearchRequest request);
}