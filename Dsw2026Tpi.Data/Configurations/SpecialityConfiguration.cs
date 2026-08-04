using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialityConfiguration
    : IEntityTypeConfiguration<Specialty>
{
    public void Configure(
        EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialities");

        builder.HasKey(speciality => speciality.Id);

        builder.Property(speciality => speciality.Id)
            .ValueGeneratedNever();

        builder.Property(speciality => speciality.Name)
            .IsRequired()
            .HasColumnType("varchar(100)");

        builder.HasIndex(speciality => speciality.Name)
            .IsUnique()
            .HasFilter("[Deleted] = 0");

        builder.Property(speciality => speciality.Description)
            .IsRequired()
            .HasColumnType("varchar(100)");

        builder.Property(speciality => speciality.Deleted)
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(speciality => speciality.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(speciality => speciality.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasQueryFilter(
            speciality => !speciality.Deleted);
    }
}