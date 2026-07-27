using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Id)
            .ValueGeneratedNever();
        
        builder.Property(d => d.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasColumnType("varchar(100)");
        
        builder.Property(d => d.LicenseNumber)
            .HasColumnName("license_number")
            .IsRequired()
            .HasColumnType("varchar(50)");

        builder.HasIndex(d => d.LicenseNumber)
            .IsUnique();
        
        builder.Property(d => d.SpecialityId)
            .HasColumnName("speciality_id")
            .IsRequired();
        
        builder.Property(d => d.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(true);
        
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired()
            .HasColumnType("datetime2");
        
        builder.HasOne(d => d.Speciality)
            .WithMany()
            .HasForeignKey(d => d.SpecialityId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasQueryFilter(d => d.IsActive);
    }
}

