using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityRuleConfiguration
    : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(
        EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable(
            "AvailabilityRules",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_AvailabilityRules_EndTimeAfterStartTime",
                    "[end_time] > [start_time]");
            });

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .ValueGeneratedNever();

        builder.Property(rule => rule.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        builder.Property(rule => rule.Year)
            .IsRequired()
            .HasColumnName("year")
            .HasConversion<short>();

        builder.Property(rule => rule.Month)
            .IsRequired()
            .HasColumnName("month")
            .HasConversion<byte>();

        builder.Property(rule => rule.DayOfWeek)
            .IsRequired()
            .HasColumnName("day_of_week")
            .HasConversion<byte>();

        builder.Property(rule => rule.StartTime)
            .IsRequired()
            .HasColumnName("start_time")
            .HasColumnType("time");

        builder.Property(rule => rule.EndTime)
            .IsRequired()
            .HasColumnName("end_time")
            .HasColumnType("time");

        builder.Property(rule => rule.Deleted)
            .IsRequired()
            .HasColumnName("deleted")
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(rule => rule.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(rule => rule.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne<Doctor>()
            .WithMany()
            .HasForeignKey(rule => rule.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restricción única del modelo de datos del TPI
        builder.HasIndex(rule => new
        {
            rule.DoctorId,
            rule.Year,
            rule.Month,
            rule.DayOfWeek,
            rule.StartTime,
            rule.EndTime
        })
        .IsUnique()
        .HasFilter("[deleted] = 0");

        builder.HasQueryFilter(
            rule => !rule.Deleted);
    }
}
