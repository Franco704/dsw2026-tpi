using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Extensions;
using Dsw2026Tpi.Application.Generators;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class AvailabilityService : IAvailabilitiesService
{
    private readonly IPersistence _persistence;
    private readonly IHolidayProvider _holidayProvider;
    public AvailabilityService(
        IPersistence persistence, IHolidayProvider holidayProvider)
    {
        _persistence = persistence;
        _holidayProvider = holidayProvider;
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> CreateAvailabilitiesAsync(
        AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(
            request,
            false);
    }

    public async Task<IEnumerable<AvailabilityModel.Response>>
        UpdateAvailabilitiesAsync(
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

        var rules = BuildMonthRules(
            request,
            today);

        var generatedAvailabilities =
            AvailabilitySlotGenerator.Generate(
                    rules,
                    currentDateTime)
                .Where(a => !_holidayProvider.IsHoliday(a.Date))
                .ToList();

        EnsureFutureSlotsWereGenerated(
            generatedAvailabilities);

        var existingAvailabilities =
            await GetExistingAvailabilitiesAsync(
                request.DoctorId,
                today);

        var existingRules =
            await GetMonthRulesAsync(
                request.DoctorId,
                today);

        if (isUpdate)
        {
            var bookedAvailabilities = existingAvailabilities
                .Where(availability => !availability.IsAvailable)
                .ToList();

            generatedAvailabilities = generatedAvailabilities
                .Where(generated =>
                    !bookedAvailabilities.Any(
                        generated.OverlapsWith))
                .ToList();

            await SoftDeleteAvailableSlotsAsync(
                existingAvailabilities);

            await SoftDeleteRulesAsync(
                existingRules);
        }
        else
        {
            EnsureMonthHasNoRules(
                existingRules);

            EnsureNoStoredConflicts(
                generatedAvailabilities,
                existingAvailabilities);
        }

        foreach (var rule in rules)
        {
            await _persistence.Add(
                rule);
        }

        await PersistGeneratedAvailabilitiesAsync(
            generatedAvailabilities);

        await _persistence.SaveChangesAsync();

        return MapResponses(
            generatedAvailabilities);
    }

    private static List<AvailabilityRule> BuildMonthRules(
        AvailabilityModel.Request request,
        DateTime today)
    {
        return request.Days
            .Select(schedule =>
                new AvailabilityRule(
                    request.DoctorId,
                    today.Year,
                    today.Month,
                    DateTimeHelpers.ParseDay(schedule.Day)!.Value,
                    schedule.StartTime,
                    schedule.EndTime))
            .ToList();
    }

    private async Task<List<AvailabilityRule>> GetMonthRulesAsync(
        Guid doctorId,
        DateTime today)
    {
        var rules =
            await _persistence.GetFiltered<AvailabilityRule>(
                rule =>
                    rule.DoctorId == doctorId &&
                    rule.Year == today.Year &&
                    rule.Month == today.Month);

        return rules?.ToList() ?? [];
    }

    private static void EnsureMonthHasNoRules(
        IReadOnlyCollection<AvailabilityRule> existingRules)
    {
        if (existingRules.Count > 0)
        {
            throw new ConflictException(
                ErrorCodes.AVAILABILITY_CONFLICT,
                nameof(ErrorCodes.AVAILABILITY_CONFLICT));
        }
    }

    private async Task SoftDeleteRulesAsync(
        IEnumerable<AvailabilityRule> existingRules)
    {
        foreach (var rule in existingRules)
        {
            rule.Delete();

            await _persistence.Update(
                rule);
        }
    }

    private async Task EnsureDoctorExistsAsync(
        Guid doctorId)
    {
        await _persistence.GetRequiredById<Doctor>(
            doctorId);
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
                    generated.OverlapsWith));

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
