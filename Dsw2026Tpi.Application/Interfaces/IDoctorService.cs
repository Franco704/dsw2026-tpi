using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<DoctorModel.Response> Create(DoctorModel.Request request);
    Task UpdateDoctors(Guid id, DoctorModel.Request request);
    Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);
    Task<List<DoctorModel.AvailiabilityResponse>> GetById(Guid id);
    Task DeleteDoctor(Guid id);
}
