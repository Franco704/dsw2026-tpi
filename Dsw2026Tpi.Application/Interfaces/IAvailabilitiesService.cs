using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilitiesService
{
    //Post
    Task<IEnumerable<AvailabilityModel.Response>> Create(AvailabilityModel.Request request);
    
    //Put
    Task<IEnumerable<AvailabilityModel.Response>> UpdateAvailability(AvailabilityModel.Request request);
    
}