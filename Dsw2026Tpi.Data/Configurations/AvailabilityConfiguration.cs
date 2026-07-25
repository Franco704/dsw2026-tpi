using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
{
    public void Configure(EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable("Availabilities");
        
        builder.HasKey(availability => availability.Id);
        
        builder.Property(a => a.Date)
            .IsRequired()
            .HasColumnType("date");
        
        builder.Property(a => a.StartTime)
            .IsRequired()
            .HasColumnType("time");
        
        builder.Property(a => a.EndTime)
            .IsRequired()
            .HasColumnType("time");
        
        builder.Property(a => a.IsAvailable)
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(true);
        
        builder.Property(a => a.Deleted)
            .HasColumnType("bit")
            .HasDefaultValue(false);
        
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");
        
        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");
        
        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasQueryFilter(a => !a.Deleted);
        
        
    }
}