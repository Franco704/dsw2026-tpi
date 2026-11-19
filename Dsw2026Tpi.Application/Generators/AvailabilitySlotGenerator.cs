using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Application.Generators;

public static class AvailabilitySlotGenerator
{
    public static IReadOnlyCollection<Availability> Generate(
        IReadOnlyCollection<AvailabilityRule> rules,
        DateTime currentDateTime)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var generatedAvailabilities =
            new List<Availability>();

        foreach (var rule in rules)
        {
            GenerateRuleAvailabilities(
                rule,
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

    private static void GenerateRuleAvailabilities(
        AvailabilityRule rule,
        DateTime currentDateTime,
        ICollection<Availability> generatedAvailabilities)
    {
        var firstDayOfMonth = new DateTime(
            rule.Year,
            rule.Month,
            1);

        var firstDate = currentDateTime.Date > firstDayOfMonth
            ? currentDateTime.Date
            : firstDayOfMonth;

        var lastDate = firstDayOfMonth
            .AddMonths(1)
            .AddDays(-1);

        for (
            var date = firstDate;
            date <= lastDate;
            date = date.AddDays(1))
        {
            if (date.DayOfWeek != rule.DayOfWeek)
            {
                continue;
            }

            GenerateDailySlots(
                rule,
                date,
                currentDateTime,
                generatedAvailabilities);
        }
    }

    private static void GenerateDailySlots(
        AvailabilityRule rule,
        DateTime date,
        DateTime currentDateTime,
        ICollection<Availability> generatedAvailabilities)
    {
        var slotDuration =
            AvailabilityRules.SlotDuration;

        for (
            var currentStart = rule.StartTime;
            currentStart < rule.EndTime;
            currentStart = currentStart.Add(slotDuration))
        {
            if (date.Add(currentStart) < currentDateTime)
            {
                continue;
            }

            generatedAvailabilities.Add(
                new Availability(
                    rule.DoctorId,
                    date,
                    currentStart,
                    currentStart.Add(slotDuration),
                    availabilityRuleId: rule.Id));
        }
    }
}
