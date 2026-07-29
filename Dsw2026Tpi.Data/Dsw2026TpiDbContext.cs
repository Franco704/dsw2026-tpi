using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Dsw2026Tpi.Data;

/// <summary>
/// Contexto principal de persistencia para las entidades del dominio.
/// </summary>
public class Dsw2026TpiDbContext : DbContext
{
    public Dsw2026TpiDbContext(
        DbContextOptions<Dsw2026TpiDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        // Aplica primero la configuración base de Entity Framework.
        base.OnModelCreating(modelBuilder);

        /*
         * Busca automáticamente todas las clases que implementan
         * IEntityTypeConfiguration<T> dentro de este ensamblado.
         *
         * De esta forma se aplican configuraciones como:
         *
         * - AppointmentConfiguration
         * - AvailabilityConfiguration
         * - DoctorConfiguration
         * - PatientConfiguration
         * - SpecialityConfiguration
         */
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly());
    }
}

/*
 * CONSIDERACIONES:
 *
 * - Este contexto administra las entidades del dominio.
 * - No contiene DbSet explícitos porque EF puede descubrir
 *   las entidades mediante las configuraciones aplicadas.
 * - AuthenticationDbContext se mantiene separado porque
 *   administra usuarios, roles y tablas de Identity.
 * - ApplyConfigurationsFromAssembly evita registrar
 *   manualmente cada configuración.
 */