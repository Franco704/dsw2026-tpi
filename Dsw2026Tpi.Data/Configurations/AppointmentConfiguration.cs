using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dsw2026Tpi.Domain.Rules;
namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration
    : IEntityTypeConfiguration<Appointment>
{
    public void Configure(
        EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(appointment => appointment.Id);

        builder.Property(appointment => appointment.Id)
            .ValueGeneratedNever();

        builder.Property(appointment => appointment.DoctorId)
            .IsRequired();

        builder.Property(appointment => appointment.PatientId)
            .IsRequired();

        builder.Property(appointment => appointment.AvailabilityId)
            .IsRequired();

        builder.Property(appointment => appointment.ScheduledAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(appointment => appointment.Reason)
            .IsRequired()
            .HasMaxLength(
    AppointmentRules.MaximumReasonLength);

        builder.Property(appointment => appointment.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);


        builder.Property(appointment => appointment.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(appointment => appointment.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        builder.HasOne(appointment => appointment.Doctor)
            .WithMany()
            .HasForeignKey(appointment => appointment.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Patient)
            .WithMany()
            .HasForeignKey(appointment => appointment.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Availability)
            .WithMany()
            .HasForeignKey(appointment => appointment.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(
                appointment => appointment.AvailabilityId)
            .IsUnique()
            .HasFilter(
                "[Status] = 'BOOKED'");
        builder.HasIndex(appointment => new
        {
            appointment.PatientId,
            appointment.ScheduledAt
        });

        builder.HasIndex(appointment => new
        {
            appointment.DoctorId,
            appointment.ScheduledAt
        });

    }
}

