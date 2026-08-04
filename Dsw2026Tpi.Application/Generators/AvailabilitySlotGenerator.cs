using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Application.Generators;

public static class AvailabilitySlotGenerator
{
    public static IReadOnlyCollection<Availability> Generate(
        Guid doctorId,
        IReadOnlyCollection<AvailabilityModel.DaySchedule> schedules,
        DateTime currentDateTime)
    {
        ArgumentNullException.ThrowIfNull(schedules);

        var today = currentDateTime.Date;

        var lastDayOfMonth = new DateTime(
            today.Year,
            today.Month,
            DateTime.DaysInMonth(
                today.Year,
                today.Month));

        var generatedAvailabilities =
            new List<Availability>();

        foreach (var schedule in schedules)
        {
            var targetDayOfWeek = DateTimeHelpers.ParseDay(
                schedule.Day);

            if (targetDayOfWeek is null)
            {
                throw new InvalidOperationException(
                    $"El día '{schedule.Day}' no fue validado correctamente.");
            }

            GenerateScheduleAvailabilities(
                doctorId,
                schedule,
                targetDayOfWeek.Value,
                today,
                lastDayOfMonth,
                currentDateTime,
                generatedAvailabilities);
        }

        return generatedAvailabilities
            .OrderBy(availability =>
                availability.Date)
            .ThenBy(availability =>
                availability.StartTime)
            .ToList();
    }

    private static void GenerateScheduleAvailabilities(
        Guid doctorId,
        AvailabilityModel.DaySchedule schedule,
        DayOfWeek targetDayOfWeek,
        DateTime firstDate,
        DateTime lastDate,
        DateTime currentDateTime,
        ICollection<Availability> generatedAvailabilities)
    {
        for (
            var date = firstDate;
            date <= lastDate;
            date = date.AddDays(1))
        {
            if (date.DayOfWeek != targetDayOfWeek)
            {
                continue;
            }

            GenerateDailySlots(
                doctorId,
                date,
                schedule.StartTime,
                schedule.EndTime,
                currentDateTime,
                generatedAvailabilities);
        }
    }

    private static void GenerateDailySlots(
        Guid doctorId,
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime,
        DateTime currentDateTime,
        ICollection<Availability> generatedAvailabilities)
    {
        var slotDuration =
            AvailabilityRules.SlotDuration;

        for (
            var currentStart = startTime;
            currentStart < endTime;
            currentStart = currentStart.Add(slotDuration))
        {
            var currentEnd = currentStart.Add(
                slotDuration);

            var slotStartDateTime = date.Date.Add(
                currentStart);

            if (slotStartDateTime < currentDateTime)
            {
                continue;
            }

            generatedAvailabilities.Add(
                new Availability(
                    doctorId,
                    date,
                    currentStart,
                    currentEnd));
        }
    }
}