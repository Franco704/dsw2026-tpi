using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration
    : IEntityTypeConfiguration<Appointment>
{
    public void Configure(
        EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        // Clave primaria
        builder.HasKey(a => a.Id);

        /*
         * El Id lo genera la entidad mediante Guid.NewGuid().
         * No queremos que SQL Server lo genere automáticamente.
         */
        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        // Claves foráneas
        builder.Property(a => a.DoctorId)
            .IsRequired();

        builder.Property(a => a.PatientId)
            .IsRequired();

        builder.Property(a => a.AvailabilityId)
            .IsRequired();

        // Fecha y hora programada del turno
        builder.Property(a => a.ScheduledAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Motivo de la consulta
        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(500);

        /*
         * Guarda el enum como texto:
         *
         * BOOKED
         * CANCELLED
         *
         * Es más legible en la base que guardar 0, 1, etc.
         */
        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Propiedades heredadas de EntityBase
        builder.Property(a => a.Deleted)
            .IsRequired()
            .HasColumnType("bit")
            .HasDefaultValue(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        builder.Property(a => a.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        /*
         * Relación:
         *
         * Doctor 1 ───── N Appointments
         *
         * No declaramos una colección Appointment en Doctor,
         * por eso usamos WithMany() vacío.
         */
        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Relación:
         *
         * Patient 1 ───── N Appointments
         *
         * Tampoco necesitamos una colección inversa en Patient.
         */
        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Relación:
         *
         * Appointment 1 ───── 1 Availability
         *
         * La FK está en Appointment:
         * Appointment.AvailabilityId
         *
         * Availability puede no tener todavía un turno,
         * por eso su propiedad Appointment es nullable.
         */
        builder.HasOne(a => a.Availability)
            .WithMany()
            .HasForeignKey(a => a.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Refuerzo en base de datos:
         * una Availability no puede asociarse a dos Appointments.
         */
        builder.HasIndex(a => a.AvailabilityId)
      .IsUnique()
      .HasFilter(
          "[Status] = 'BOOKED' AND [Deleted] = 0");

        /*
         * Índices para las consultas más habituales:
         *
         * - turnos de un paciente ordenados/filtrados por fecha;
         * - turnos de un médico según fecha.
         */
        builder.HasIndex(a => new
        {
            a.PatientId,
            a.ScheduledAt
        });

        builder.HasIndex(a => new
        {
            a.DoctorId,
            a.ScheduledAt
        });

        /*
         * Soft delete global.
         * Las consultas normales no devolverán turnos
         * con Deleted = true.
         */
        builder.HasQueryFilter(a => !a.Deleted);
    }
}