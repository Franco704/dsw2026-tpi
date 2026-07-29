using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

/// <summary>
/// Configura el mapeo entre la entidad Patient
/// y la tabla Patients de la base de datos.
/// </summary>
public class PatientConfiguration
    : IEntityTypeConfiguration<Patient>
{
    /// <summary>
    /// Define las columnas, restricciones, índices y filtros
    /// correspondientes a la entidad Patient.
    /// </summary>
    public void Configure(
        EntityTypeBuilder<Patient> builder)
    {
        // Asocia la entidad Patient con la tabla física Patients.
        builder.ToTable("Patients");

        // Define Id como clave primaria de la tabla.
        builder.HasKey(patient => patient.Id);

        /*
         * El identificador se genera en el dominio mediante
         * EntityBase y Guid.NewGuid().
         *
         * SQL Server no debe generar este valor automáticamente.
         */
        builder.Property(patient => patient.Id)
            .ValueGeneratedNever();

        /*
         * Almacena el identificador del usuario de ASP.NET Identity
         * asociado al paciente.
         *
         * Se utiliza string porque IdentityUser emplea string
         * como tipo de clave predeterminado.
         */
        builder.Property(patient => patient.UserId)
            .IsRequired()
            .HasColumnName("user_id")
            .HasMaxLength(450);

        /*
         * Garantiza que un usuario de Identity solamente pueda
         * estar asociado a un único perfil de paciente.
         *
         * Esta restricción representa la cardinalidad:
         *
         * User 1 ───── 0..1 Patient
         */
        builder.HasIndex(patient => patient.UserId)
            .IsUnique();

        /*
         * Configura el DNI del paciente.
         *
         * Se almacena como varchar porque representa un
         * identificador y no una magnitud numérica.
         */
        builder.Property(patient => patient.Dni)
            .IsRequired()
            .HasColumnName("dni")
            .HasColumnType("varchar(10)");

        /*
         * Impide que dos pacientes distintos sean registrados
         * con el mismo DNI.
         */
        builder.HasIndex(patient => patient.Dni)
            .IsUnique();

        /*
         * Configura el nombre completo del paciente.
         *
         * Es opcional porque el paciente puede crearse durante
         * su primer acceso utilizando únicamente email y DNI.
         */
        builder.Property(patient => patient.FullName)
            .HasColumnName("full_name")
            .HasColumnType("varchar(150)")
            .IsRequired(false);

        /*
         * Configura la marca de eliminación lógica heredada
         * de EntityBase.
         *
         * Todo paciente nuevo comienza con Deleted = false.
         */
        builder.Property(patient => patient.Deleted)
            .IsRequired()
            .HasColumnName("deleted")
            .HasColumnType("bit")
            .HasDefaultValue(false);

        /*
         * Configura la fecha de creación heredada de EntityBase.
         */
        builder.Property(patient => patient.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        /*
         * Configura la fecha de última modificación
         * heredada de EntityBase.
         */
        builder.Property(patient => patient.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        /*
         * Filtro global de eliminación lógica.
         *
         * EF Core agrega automáticamente la condición
         * Deleted = false a las consultas normales de Patient.
         *
         * Los pacientes eliminados solamente podrán consultarse
         * mediante IgnoreQueryFilters().
         */
        builder.HasQueryFilter(
            patient => !patient.Deleted);
    }
}