namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class DateTimeHelpers
{
    private static readonly Dictionary<DayOfWeek, string> DaysEspaniol = new()
    {
        { DayOfWeek.Monday, "LUNES" },
        { DayOfWeek.Tuesday, "MARTES" },
        { DayOfWeek.Wednesday, "MIÉRCOLES" },
        { DayOfWeek.Thursday, "JUEVES" },
        { DayOfWeek.Friday, "VIERNES" },
        { DayOfWeek.Saturday, "SÁBADO" },
        { DayOfWeek.Sunday, "DOMINGO" }
    };

    
    public static string ToSpanish(this DayOfWeek day)
    {
        return DaysEspaniol.TryGetValue(day, out var dayName) ? dayName : day.ToString();
    }

    
    public static DayOfWeek? ParseSpanish(string day)
    {
        var norm = day.Trim().ToUpperInvariant();
        var e = DaysEspaniol.FirstOrDefault(x => x.Value == norm);
        return e.Value != null ? e.Key : null;
    }

    
    public static DayOfWeek? ParseDay(string day)
    {
        if (Enum.TryParse<DayOfWeek>(day, true, out var dayOfWeek))
            return dayOfWeek;
        
        return ParseSpanish(day);
    }

    public static string ToTimeString(this TimeSpan time)
    {
        return time.ToString(@"hh\:mm");
    }
    
}
