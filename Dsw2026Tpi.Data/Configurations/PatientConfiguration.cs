using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

// Define cómo se persiste Patient en la base de datos.
public class PatientConfiguration
    : IEntityTypeConfiguration<Patient>
{
    public void Configure(
        EntityTypeBuilder<Patient> builder)
    {
        // Define el nombre de la tabla.
        builder.ToTable("Patients");

        // Define la clave primaria de la entidad.
        builder.HasKey(patient => patient.Id);

        // El identificador es generado por EntityBase.
        builder.Property(patient => patient.Id)
            .ValueGeneratedNever();

        // Relaciona al paciente con el Id de Identity.
        builder.Property(patient => patient.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // Un usuario solamente puede tener un perfil de paciente.
        builder.HasIndex(patient => patient.UserId)
            .IsUnique();

        // El DNI se almacena como varchar según el modelo.
        builder.Property(patient => patient.Dni)
            .IsRequired()
            .HasColumnType("varchar(10)");

        // Evita registrar dos pacientes con el mismo DNI
        builder.HasIndex(patient => patient.Dni)
            .IsUnique();

        // El nombre es opcional porque el login no lo proporciona.
        builder.Property(patient => patient.FullName)
            .HasColumnType("varchar(150)")
            .IsRequired(false);

        // Configura la eliminación lógica.
        builder.Property(patient => patient.Deleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Configura la fecha de creación heredada.
        builder.Property(patient => patient.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Se conserva UpdatedAt porque pertenece a EntityBase.
        builder.Property(patient => patient.UpdatedAt)
            .IsRequired()
            .HasColumnType("datetime2");
    }
}