using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialitiesService
{
    Task<Pagination<SpecialtyModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);

    Task<SpecialtyModel.Response> Create(SpecialtyModel.Request request);

    Task<SpecialtyModel.Response> UpdateSpecialitiy(Guid id, SpecialtyModel.Request request);
    
    Task DeleteSpecialitiy(Guid id);
}