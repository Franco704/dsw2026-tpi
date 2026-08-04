using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Generators;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilitiesService
{
    private readonly IPersistence _persistence;
    private readonly IFeriadoProvider _feriado;
    public AvailabilityService(
        IPersistence persistence, IFeriadoProvider feriadoProvider)
    {
        _persistence = persistence;
        _feriado = feriadoProvider;
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> Create(
        AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(
            request,
            false);
    }

    public async Task<IEnumerable<AvailabilityModel.Response>>
        UpdateAvailability(
            AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(
            request,
            true);
    }

    private async Task<IEnumerable<AvailabilityModel.Response>>
        ProcessAvailabilitiesAsync(
            AvailabilityModel.Request request,
            bool isUpdate)
    {
        AvailabilityRequestValidator.Validate(
            request);

        await EnsureDoctorExistsAsync(
            request.DoctorId);

        var currentDateTime = DateTime.Now;
        var today = currentDateTime.Date;

        var generatedAvailabilities =
            AvailabilitySlotGenerator.Generate(
                    request.DoctorId,
                    request.Days,
                    currentDateTime)
                    .Where(a => !_feriado.EsFeriado(a.Date))
                .ToList();
        EnsureFutureSlotsWereGenerated (generatedAvailabilities);

        var existingAvailabilities =
            await GetExistingAvailabilitiesAsync(
                request.DoctorId,
                today);

        var protectedAvailabilities = isUpdate
            ? existingAvailabilities
                .Where(availability =>
                    !availability.IsAvailable)
                .ToList()
            : existingAvailabilities;

        EnsureNoStoredConflicts(
            generatedAvailabilities,
            protectedAvailabilities);

        if (isUpdate)
        {
            await SoftDeleteAvailableSlotsAsync(
                existingAvailabilities);
        }

        await PersistGeneratedAvailabilitiesAsync(
            generatedAvailabilities);

        return MapResponses(
            generatedAvailabilities);
    }

    private async Task EnsureDoctorExistsAsync(
        Guid doctorId)
    {
        var doctor = await _persistence.GetById<Doctor>(
            doctorId);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }
    }

    private async Task<List<Availability>>
        GetExistingAvailabilitiesAsync(
            Guid doctorId,
            DateTime today)
    {
        var firstDayOfNextMonth = new DateTime(
                today.Year,
                today.Month,
                1)
            .AddMonths(1);

        var availabilities =
            await _persistence.GetFiltered<Availability>(
                availability =>
                    availability.DoctorId == doctorId &&
                    availability.Date >= today &&
                    availability.Date < firstDayOfNextMonth);

        return availabilities?.ToList() ?? [];
    }

    private static void EnsureNoStoredConflicts(
        IReadOnlyCollection<Availability> generatedAvailabilities,
        IReadOnlyCollection<Availability> storedAvailabilities)
    {
        var hasConflict = generatedAvailabilities.Any(
            generated =>
                storedAvailabilities.Any(
                    stored =>
                        generated.Date == stored.Date &&
                        generated.StartTime < stored.EndTime &&
                        generated.EndTime > stored.StartTime));

        if (hasConflict)
        {
            throw new ConflictException(
                ErrorCodes.AVAILABILITY_CONFLICT,
                nameof(ErrorCodes.AVAILABILITY_CONFLICT));
        }
    }

    private async Task SoftDeleteAvailableSlotsAsync(
        IEnumerable<Availability> existingAvailabilities)
    {
        foreach (var availability in existingAvailabilities)
        {
            if (!availability.IsAvailable)
            {
                continue;
            }

            availability.Delete();

            await _persistence.Update(
                availability);
        }
    }

    private async Task PersistGeneratedAvailabilitiesAsync(
        IEnumerable<Availability> generatedAvailabilities)
    {
        foreach (var availability in generatedAvailabilities)
        {
            await _persistence.Add(
                availability);
        }
    }

    private static IEnumerable<AvailabilityModel.Response>
        MapResponses(
            IEnumerable<Availability> availabilities)
    {
        return availabilities.Select(
            availability =>
                new AvailabilityModel.Response(
                    availability.Id,
                    availability.DoctorId,
                    availability.Date,
                    availability.StartTime,
                    availability.EndTime));
    }

    private static void EnsureFutureSlotsWereGenerated(
        IReadOnlyCollection<Availability> generatedAvailabilities)
    {
        if (generatedAvailabilities.Count > 0)
        {
            return;
        }

        var validation = new ValidationException();

        validation.WithDetail(
            "days",
            "no_future_slots_available_in_current_month");

        throw validation;
    }
}
