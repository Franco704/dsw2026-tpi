using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Application.Validators;

public static class AvailabilityRequestValidator
{
    

    private sealed record ValidatedSchedule(
        int Index,
        DayOfWeek Day,
        TimeSpan StartTime,
        TimeSpan EndTime);

    public static void Validate(AvailabilityModel.Request? request)
    {
        var validation = new ValidationException();

        if (request is null)
        {
            validation.WithDetail(
                "request",
                "required");

            throw validation;
        }

        ValidateDoctorId(
            request.DoctorId,
            validation);

        ValidateDays(
            request.Days,
            validation);

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }

    private static void ValidateDoctorId(
        Guid doctorId,
        ValidationException validation)
    {
        if (doctorId == Guid.Empty)
        {
            validation.WithDetail(
                "doctorId",
                "required");
        }
    }

    private static void ValidateDays(
        IReadOnlyCollection<AvailabilityModel.DaySchedule>? days,
        ValidationException validation)
    {
        if (days is null || days.Count == 0)
        {
            validation.WithDetail(
                "days",
                "required");

            return;
        }

        var validSchedules = new List<ValidatedSchedule>();
        var index = 0;

        foreach (var schedule in days)
        {
            var validatedSchedule = ValidateSchedule(
                schedule,
                index,
                validation);

            if (validatedSchedule is not null)
            {
                validSchedules.Add(validatedSchedule);
            }

            index++;
        }

        ValidateOverlappingRanges(
            validSchedules,
            validation);
    }

    private static ValidatedSchedule? ValidateSchedule(
        AvailabilityModel.DaySchedule? schedule,
        int index,
        ValidationException validation)
    {
        var fieldPrefix = $"days[{index}]";

        if (schedule is null)
        {
            validation.WithDetail(
                fieldPrefix,
                "required");

            return null;
        }

        var parsedDay = ValidateDay(
            schedule.Day,
            fieldPrefix,
            validation);

        var validTimeRange = ValidateTimeRange(
            schedule.StartTime,
            schedule.EndTime,
            fieldPrefix,
            validation);

        if (parsedDay is null || !validTimeRange)
        {
            return null;
        }

        return new ValidatedSchedule(
            index,
            parsedDay.Value,
            schedule.StartTime,
            schedule.EndTime);
    }

    private static DayOfWeek? ValidateDay(
        string? day,
        string fieldPrefix,
        ValidationException validation)
    {
        if (string.IsNullOrWhiteSpace(day))
        {
            validation.WithDetail(
                $"{fieldPrefix}.day",
                "required");

            return null;
        }

        var parsedDay = DateTimeHelpers.ParseDay(day);

        if (parsedDay is null)
        {
            validation.WithDetail(
                $"{fieldPrefix}.day",
                "invalid_day");
        }

        return parsedDay;
    }

    private static bool ValidateTimeRange(
        TimeSpan startTime,
        TimeSpan endTime,
        string fieldPrefix,
        ValidationException validation)
    {
        if (startTime >= endTime)
        {
            validation.WithDetail(
                $"{fieldPrefix}.endTime",
                "must_be_after_start_time");

            return false;
        }

        var duration = endTime - startTime;
        var slotDuration = AvailabilityRules.SlotDuration;

        if (duration.Ticks % slotDuration.Ticks != 0)
        {
            validation.WithDetail(
                $"{fieldPrefix}.endTime",
                "range_must_be_divisible_into_30_minute_slots");

            return false;
        }

        return true;
    }

    private static void ValidateOverlappingRanges(
        IReadOnlyCollection<ValidatedSchedule> schedules,
        ValidationException validation)
    {
        foreach (var dayGroup in schedules.GroupBy(
            schedule => schedule.Day))
        {
            var orderedRanges = dayGroup
                .OrderBy(schedule => schedule.StartTime)
                .ThenBy(schedule => schedule.EndTime)
                .ToList();

            if (orderedRanges.Count == 0)
            {
                continue;
            }

            var latestEndTime = orderedRanges[0].EndTime;

            for (var index = 1; index < orderedRanges.Count; index++)
            {
                var currentRange = orderedRanges[index];

                if (currentRange.StartTime < latestEndTime)
                {
                    validation.WithDetail(
                        $"days[{currentRange.Index}].startTime",
                        "overlapping_time_range");
                }

                if (currentRange.EndTime > latestEndTime)
                {
                    latestEndTime = currentRange.EndTime;
                }
            }
        }
    }
}