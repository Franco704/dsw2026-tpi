using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class PatientConfiguration
    : IEntityTypeConfiguration<Patient>
{
    public void Configure(
        EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.Id)
            .ValueGeneratedNever();

        builder.Property(patient => patient.UserId)
            .IsRequired()
            .HasColumnName("user_id")
            .HasMaxLength(450);

        builder.HasIndex(patient => patient.UserId)
            .IsUnique();

        builder.Property(patient => patient.Dni)
            .IsRequired()
            .HasColumnName("dni")
            .HasColumnType("varchar(10)");

        builder.HasIndex(patient => patient.Dni)
            .IsUnique();

        builder.Property(patient => patient.FullName)
            .HasColumnName("full_name")
            .HasColumnType("varchar(150)")
            .IsRequired(false);

        builder.Property(patient => patient.Deleted)
            .IsRequired()
            .HasColumnName("deleted")
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(patient => patient.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(patient => patient.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasQueryFilter(
            patient => !patient.Deleted);
    }
}