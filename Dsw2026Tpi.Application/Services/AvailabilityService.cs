using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilitiesService
{
    private readonly IPersistence _persistence;
    
    public AvailabilityService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> Create(AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(request, false);
    }
    
    public async Task<IEnumerable<AvailabilityModel.Response>> UpdateAvailability(AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(request, true);
    }

    private async Task<IEnumerable<AvailabilityModel.Response>> ProcessAvailabilitiesAsync(AvailabilityModel.Request request, 
        bool isUpdate)
    {
        var doctor = await _persistence.GetById<Doctor>(request.DoctorId);
        if (doctor == null) 
            throw new KeyNotFoundException($"No existe el doctor con la ID {request.DoctorId}");

        if (request.Days == null || !request.Days.Any())
            throw new ArgumentException("Debe especificarse al menos un dia con sus horarios");

        var today = DateTime.Today;
        var fDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var lDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        
        var existingAvailabilities = (await _persistence.GetFiltered<Availability>(a =>
            a.DoctorId == request.DoctorId &&
            a.Date >= fDayOfMonth &&
            a.Date <= lDayOfMonth))?.ToList() ?? new List<Availability>();

        if (isUpdate)
        {
            foreach (var e in existingAvailabilities)
            {
                if (e.IsAvailable)
                {
                    e.Deleted = true;
                    e.UpdatedAt = DateTime.UtcNow;
                    await _persistence.Update(e);
                }
            }
            existingAvailabilities = existingAvailabilities.Where(a => !a.IsAvailable).ToList();
        }

        var newAvailabilities = new List<Availability>();

        foreach (var dR in request.Days)
        {
            if (dR.StartTime >= dR.EndTime)
                throw new ArgumentException("La hora de inicio debe ser menor a la hora de fin");
            
            var targetDayOfWeek = DateTimeHelpers.ParseDay(dR.Day);
            if (targetDayOfWeek == null)
                throw new ArgumentException($"El día {dR.Day} no es valido.");
            
            for (var date = fDayOfMonth; date <= lDayOfMonth; date = date.AddDays(1))
            {
                if (date >= today && date.DayOfWeek == targetDayOfWeek)
                {
                    var currentStart = dR.StartTime;

                    while (currentStart < dR.EndTime)
                    {
                        var currentEnd = currentStart.Add(TimeSpan.FromMinutes(30));
                        if (currentEnd > dR.EndTime) currentEnd = dR.EndTime;
                        
                        var isOcupied = existingAvailabilities.Any(a=>
                            a.Date == date &&
                            currentStart < a.EndTime &&
                            currentEnd > a.StartTime);

                        if (!isOcupied)
                        {
                            var availability = new Availability(
                                request.DoctorId,
                                date,
                                currentStart,
                                currentEnd);
                            newAvailabilities.Add(availability);
                        }
                        else
                        {
                            throw new InvalidOperationException("Se detectó solapamiento de horarios.");
                        }
                        currentStart = currentEnd;
                    }
                }
            }

        }

        foreach (var av in newAvailabilities)
        {
            await _persistence.Add(av);
        }

        return newAvailabilities.Select(a => new AvailabilityModel.Response(
            a.Id,
            a.DoctorId,
            a.Date,
            a.StartTime,
            a.EndTime));
    }
}