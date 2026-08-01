using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialitiesService
{
    //Get
    Task<Pagination<SpecialtyModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);

    //Post
    Task<SpecialtyModel.Response> Create(SpecialtyModel.Request request);

    //Put
    Task<SpecialtyModel.Response> UpdateSpecialitiy(Guid id, SpecialtyModel.Request request);
    
    //Delete
    Task DeleteSpecialitiy(Guid id);
}