using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

/// <summary>
/// Configura el mapeo entre la entidad Speciality
/// y la tabla Specialities de la base de datos.
/// </summary>
public class SpecialityConfiguration
    : IEntityTypeConfiguration<Speciality>
{
    /// <summary>
    /// Define las columnas, restricciones, índices
    /// y filtros globales de la entidad Speciality.
    /// </summary>
    public void Configure(
        EntityTypeBuilder<Speciality> builder)
    {
        // Asocia la entidad con la tabla física Specialities.
        builder.ToTable("Specialities");

        // Define Id como clave primaria de la tabla.
        builder.HasKey(speciality => speciality.Id);

        /*
         * El identificador se genera en el dominio mediante
         * EntityBase y Guid.NewGuid().
         *
         * SQL Server no debe generar este valor automáticamente.
         */
        builder.Property(speciality => speciality.Id)
            .ValueGeneratedNever();

        /*
         * Configura el nombre de la especialidad.
         *
         * Es obligatorio y admite hasta 100 caracteres.
         */
        builder.Property(speciality => speciality.Name)
            .IsRequired()
            .HasColumnType("varchar(100)");

        /*
         * Garantiza que no existan dos especialidades activas
         * con el mismo nombre.
         *
         * El filtro permite reutilizar el nombre de una
         * especialidad que fue eliminada lógicamente.
         */
        builder.HasIndex(speciality => speciality.Name)
            .IsUnique()
            .HasFilter("[Deleted] = 0");

        /*
         * Configura la descripción de la especialidad.
         *
         * Es obligatoria y admite hasta 100 caracteres.
         */
        builder.Property(speciality => speciality.Description)
            .IsRequired()
            .HasColumnType("varchar(100)");

        /*
         * Configura la marca de eliminación lógica
         * heredada de EntityBase.
         *
         * Toda especialidad nueva comienza activa,
         * con Deleted = false.
         */
        builder.Property(speciality => speciality.Deleted)
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(false);

        /*
         * Configura la fecha de creación
         * heredada de EntityBase.
         */
        builder.Property(speciality => speciality.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        /*
         * Configura la fecha de última modificación
         * heredada de EntityBase.
         */
        builder.Property(speciality => speciality.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        /*
         * Filtro global de eliminación lógica.
         *
         * EF Core agrega automáticamente la condición
         * Deleted = false a las consultas normales.
         *
         * Las especialidades eliminadas solamente podrán
         * consultarse mediante IgnoreQueryFilters().
         */
        builder.HasQueryFilter(
            speciality => !speciality.Deleted);
    }
}