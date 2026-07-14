using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialitiesService : ISpecialitiesService
{
    private readonly IPersistence _persistence;
    
    public SpecialitiesService(IPersistence persistence)
    {
        _persistence = persistence;
    }
    
    public Task UpdateSpecialitiy(Guid id, SpecialityModel.Request request)
    {
        throw new NotImplementedException();
    }

    public Task DeleteSpecialitiy(Guid id)
    {
    throw new NotImplementedException();
    }
}