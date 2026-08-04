using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialitiesService
{
    Task<Pagination<SpecialtyModel.Response>> GetAllSpecialtiesAsync(
        int pageSize,
        int pageIndex,
        string? name = null);

    Task<SpecialtyModel.Response> CreateSpecialtyAsync(
        SpecialtyModel.Request request);

    Task<SpecialtyModel.Response> UpdateSpecialtyAsync(
        Guid specialtyId,
        SpecialtyModel.Request request);

    Task DeleteSpecialtyAsync(
        Guid specialtyId);
}