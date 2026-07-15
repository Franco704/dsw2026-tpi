using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialitiesService
{
    //Get
    Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null);

    //Post
    Task<SpecialityModel.Response> Create(SpecialityModel.Request request);

    //Put
    Task<SpecialityModel.Response> UpdateSpecialitiy(Guid id, SpecialityModel.Request request);
    
    //Delete
    Task DeleteSpecialitiy(Guid id);
}