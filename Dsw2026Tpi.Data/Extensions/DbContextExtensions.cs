using Dsw2026Tpi.Data.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Dsw2026Tpi.Data.Extensions;

/// <summary>
/// Extensiones para cargar datos iniciales desde archivos JSON.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Inserta entidades de tipo T cuando la tabla está vacía.
    /// </summary>
    public static void Seedwork<T>(
        this DbContext context,
        string dataSource)
        where T : class
    {
        // Evita duplicar datos si la tabla ya contiene registros.
        if (context.Set<T>().Any())
        {
            return;
        }

        // Construye la ruta y lee el archivo JSON.
        var filePath = Path.Combine(
            AppContext.BaseDirectory,
            dataSource);

        var json = File.ReadAllText(filePath);

        // Convierte el JSON en una colección de entidades.
        var entities = JsonSerializer.Deserialize<List<T>>(
            json,
            JsonOptions.JsonSerializerOptions);

        // Finaliza si el archivo no contiene datos válidos.
        if (entities is null || entities.Count == 0)
        {
            return;
        }

        // Agrega las entidades y confirma los cambios.
        context.Set<T>().AddRange(entities);
        context.SaveChanges();
    }
}

/*
 * CONSIDERACIONES:
 *
 * - Solo carga datos cuando la tabla está completamente vacía.
 * - Si el archivo no existe o el JSON es inválido, se lanza una excepción.
 * - El método es sincrónico.
 * - Seedwork podría llamarse SeedFromJson para expresar mejor su función.
 */