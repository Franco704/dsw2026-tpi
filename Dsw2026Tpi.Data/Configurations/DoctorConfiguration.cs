using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

/// <summary>
/// Configura el mapeo entre la entidad Doctor
/// y la tabla Doctors de la base de datos.
/// </summary>
public class DoctorConfiguration
    : IEntityTypeConfiguration<Doctor>
{
    /// <summary>
    /// Define las columnas, restricciones, relaciones,
    /// índices y filtros correspondientes a Doctor.
    /// </summary>
    public void Configure(
        EntityTypeBuilder<Doctor> builder)
    {
        // Asocia la entidad Doctor con la tabla física Doctors.
        builder.ToTable("Doctors");

        // Define Id como clave primaria de la tabla.
        builder.HasKey(doctor => doctor.Id);

        /*
         * El identificador se genera en el dominio mediante
         * EntityBase y Guid.NewGuid().
         *
         * SQL Server no debe generar este valor automáticamente.
         */
        builder.Property(doctor => doctor.Id)
            .ValueGeneratedNever();

        /*
         * Configura el nombre completo del médico.
         *
         * Es obligatorio y admite hasta 100 caracteres.
         */
        builder.Property(doctor => doctor.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("varchar(100)");

        /*
         * Configura el número de matrícula profesional.
         *
         * Es obligatorio y admite hasta 50 caracteres.
         */
        builder.Property(doctor => doctor.LicenseNumber)
            .HasColumnName("license_number")
            .IsRequired()
            .HasColumnType("varchar(50)");

        /*
         * Garantiza que no puedan registrarse dos médicos
         * con el mismo número de matrícula.
         *
         * Esta restricción fue agregada por la implementación
         * para proteger la identidad profesional del médico.
         */
        builder.HasIndex(doctor => doctor.LicenseNumber)
            .IsUnique();

        /*
         * Configura la clave foránea de la especialidad.
         *
         * Todo médico debe pertenecer a una especialidad.
         */
        builder.Property(doctor => doctor.SpecialityId)
            .HasColumnName("speciality_id")
            .IsRequired();

        /*
         * Indica si el médico se encuentra operativo.
         *
         * Todo médico nuevo comienza activo.
         *
         * Esta propiedad fue agregada por la implementación
         * y no aparece expresamente en el modelo original.
         */
        builder.Property(doctor => doctor.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(true);

        /*
         * Configura la marca de eliminación lógica
         * heredada de EntityBase.
         *
         * Todo médico nuevo comienza con Deleted = false.
         */
        builder.Property(doctor => doctor.Deleted)
            .HasColumnName("deleted")
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(false);

        /*
         * Configura la fecha de creación
         * heredada de EntityBase.
         */
        builder.Property(doctor => doctor.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasColumnType("datetime2");

        /*
         * Configura la fecha de última modificación
         * heredada de EntityBase.
         */
        builder.Property(doctor => doctor.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasColumnType("datetime2");

        /*
         * Relación:
         *
         * Speciality 1 ───── N Doctors
         *
         * Cada médico pertenece a una especialidad.
         * Una especialidad puede tener múltiples médicos.
         *
         * WithMany() se deja vacío porque Speciality no expone
         * una colección inversa de Doctor.
         *
         * Restrict evita eliminar físicamente una especialidad
         * que tenga médicos asociados.
         */
        builder.HasOne(doctor => doctor.Speciality)
            .WithMany()
            .HasForeignKey(doctor => doctor.SpecialityId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Filtro global para médicos inactivos.
         *
         * EF Core agrega automáticamente la condición
         * IsActive = true a las consultas normales.
         *
         * Actualmente el filtro conserva la funcionalidad
         * original y no evalúa explícitamente Deleted.
         */
        builder.HasQueryFilter(
            doctor => doctor.IsActive);
    }
}
/*El principal punto pendiente es decidir si el filtro definitivo debería ser:

doctor => doctor.IsActive && !doctor.Deleted

o si corresponde eliminar una de las dos propiedades para evitar representar dos veces el mismo estado.*/