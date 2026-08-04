using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityConfiguration
    : IEntityTypeConfiguration<Availability>
{
    public void Configure(
        EntityTypeBuilder<Availability> builder)
    {
        builder.ToTable(
            "Availabilities",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Availabilities_EndTimeAfterStartTime",
                    "[end_time] > [start_time]");
            });

        builder.HasKey(availability => availability.Id);

        builder.Property(availability => availability.Id)
            .ValueGeneratedNever();

        builder.Property(availability => availability.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        builder.Property(availability => availability.Date)
            .IsRequired()
            .HasColumnName("slot_date")
            .HasColumnType("date");

        builder.Property(availability => availability.StartTime)
            .IsRequired()
            .HasColumnName("start_time")
            .HasColumnType("time");

        builder.Property(availability => availability.EndTime)
            .IsRequired()
            .HasColumnName("end_time")
            .HasColumnType("time");

        builder.Property(availability => availability.IsAvailable)
            .IsRequired()
            .HasColumnName("is_available")
            .HasColumnType("bit")
            .HasDefaultValue(true);

        builder.Property(availability => availability.Deleted)
            .IsRequired()
            .HasColumnName("deleted")
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(availability => availability.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(availability => availability.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(availability => availability.Doctor)
            .WithMany()
            .HasForeignKey(availability => availability.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(availability => new
        {
            availability.DoctorId,
            availability.Date,
            availability.StartTime
        })
        .IsUnique()
        .HasFilter("[deleted] = 0");

        builder.HasIndex(availability => new
        {
            availability.DoctorId,
            availability.Date,
            availability.StartTime,
            availability.EndTime
        });

        builder.HasQueryFilter(
            availability => !availability.Deleted);
    }
}
