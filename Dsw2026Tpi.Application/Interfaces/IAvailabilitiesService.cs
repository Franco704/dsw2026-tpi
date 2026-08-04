using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilitiesService
{
    Task<IEnumerable<AvailabilityModel.Response>> Create(AvailabilityModel.Request request);
    
    Task<IEnumerable<AvailabilityModel.Response>> UpdateAvailability(AvailabilityModel.Request request);
    
}