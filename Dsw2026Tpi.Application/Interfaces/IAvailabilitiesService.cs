using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IAvailabilitiesService
{
    Task<IEnumerable<AvailabilityModel.Response>>
        CreateAvailabilitiesAsync(
            AvailabilityModel.Request request);

    Task<IEnumerable<AvailabilityModel.Response>>
        UpdateAvailabilitiesAsync(
            AvailabilityModel.Request request);
}