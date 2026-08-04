using Dsw2026Tpi.Data.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dsw2026Tpi.Data.Extensions;

public static class DbContextExtensions
{
    public static void Seedwork<T>(
        this DbContext context,
        string dataSource)
        where T : class
    {
        if (context.Set<T>().Any())
        {
            return;
        }

        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            dataSource);

        var json = File.ReadAllText(filePath);

        var entities = JsonSerializer.Deserialize<List<T>>(
            json,
            JsonOptions.JsonSerializerOptions);

        if (entities is null || entities.Count == 0)
        {
            return;
        }

        context.Set<T>().AddRange(entities);
        context.SaveChanges();
    }
}
