using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class DoctorConfiguration
    : IEntityTypeConfiguration<Doctor>
{
    public void Configure(
        EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(doctor => doctor.Id);

        builder.Property(doctor => doctor.Id)
            .ValueGeneratedNever();

        builder.Property(doctor => doctor.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("varchar(100)");

        builder.Property(doctor => doctor.LicenseNumber)
            .HasColumnName("license_number")
            .IsRequired()
            .HasColumnType("varchar(50)");

        builder.HasIndex(doctor => doctor.LicenseNumber)
            .IsUnique();

        builder.Property(doctor => doctor.SpecialityId)
            .HasColumnName("speciality_id")
            .IsRequired();

        builder.Property(doctor => doctor.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(true);

        builder.Property(doctor => doctor.Deleted)
            .HasColumnName("deleted")
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(doctor => doctor.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(doctor => doctor.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasColumnType("datetime2");

        builder.HasOne(doctor => doctor.Speciality)
            .WithMany()
            .HasForeignKey(doctor => doctor.SpecialityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(
            doctor => doctor.IsActive);
    }
}

