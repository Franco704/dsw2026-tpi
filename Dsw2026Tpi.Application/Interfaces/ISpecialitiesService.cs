using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialitiesService
{
    //Get
    
    //Post

    
    //Put
    Task UpdateSpecialitiy(Guid id, SpecialityModel.Request request);
    
    //Delete
    Task DeleteSpecialitiy(Guid id);
}