using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace Dsw2026Tpi.Data
{
    public class HolidayProvider : IHolidayProvider
    {
        private readonly HashSet<DateTime> _holidays;

        public HolidayProvider()
        {

            var path = Path.Combine(AppContext.BaseDirectory, "Sources", "Holidays.json");
            var json = File.ReadAllText(path);
            var holidays = JsonSerializer.Deserialize<List<Holiday>>(json
                , new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })
                ?? new List<Holiday>();
            _holidays = holidays.Select(holiday => holiday.Date.Date).ToHashSet();
        }
        public bool IsHoliday(DateTime date) => _holidays.Contains(date);
    }
}
