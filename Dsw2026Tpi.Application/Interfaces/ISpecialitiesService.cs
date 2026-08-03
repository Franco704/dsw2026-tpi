using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialitiesService
{
    //Get
    Task<Pagination<SpecialtyModel.Response>> GetAllSpecialtyAsync(int pageSize, int pageIndex, string? name = null);

    //Post
    Task<SpecialtyModel.Response> CreateSpecialtyAsync(SpecialtyModel.Request request);

    //Put
    Task<SpecialtyModel.Response> UpdateSpecialtyAsync(Guid id, SpecialtyModel.Request request);
    
    //Delete
    Task DeleteSpecialtyAsync(Guid id);
}