using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<DoctorModel.Response> CreateDoctorAsync(
        DoctorModel.Request request);

    Task<DoctorModel.Response> UpdateDoctorAsync(
        Guid doctorId,
        DoctorModel.Request request);

    Task<Pagination<DoctorModel.Response>> GetAllDoctorsAsync(
        int pageSize,
        int pageIndex,
        string? name = null);

    Task<List<DoctorModel.AvailabilityResponse>>
        GetDoctorAvailabilitiesAsync(
            Guid doctorId);

    Task DeleteDoctorAsync(
        Guid doctorId);
}
