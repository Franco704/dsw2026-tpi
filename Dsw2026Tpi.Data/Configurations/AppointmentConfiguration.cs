using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

/// <summary>
/// Configura el mapeo entre la entidad Appointment
/// y la tabla Appointments de la base de datos.
/// </summary>
public class AppointmentConfiguration
    : IEntityTypeConfiguration<Appointment>
{
    /// <summary>
    /// Define columnas, restricciones, relaciones,
    /// índices y filtros globales de la entidad Appointment.
    /// </summary>
    public void Configure(
        EntityTypeBuilder<Appointment> builder)
    {
        // Asocia la entidad con la tabla física Appointments.
        builder.ToTable("Appointments");

        // Define Id como clave primaria de la tabla.
        builder.HasKey(appointment => appointment.Id);

        /*
         * El identificador se genera en el dominio mediante
         * Guid.NewGuid(), por lo que SQL Server no debe
         * generar un valor automáticamente.
         */
        builder.Property(appointment => appointment.Id)
            .ValueGeneratedNever();

        /*
         * Define DoctorId como una clave foránea obligatoria.
         * Todo turno debe estar asociado a un médico.
         */
        builder.Property(appointment => appointment.DoctorId)
            .IsRequired();

        /*
         * Define PatientId como una clave foránea obligatoria.
         * Todo turno debe pertenecer a un paciente.
         */
        builder.Property(appointment => appointment.PatientId)
            .IsRequired();

        /*
         * Define AvailabilityId como una clave foránea obligatoria.
         * Todo turno debe originarse en una disponibilidad concreta.
         */
        builder.Property(appointment => appointment.AvailabilityId)
            .IsRequired();

        /*
         * Almacena la fecha y hora programada del turno.
         * Se utiliza datetime2 para conservar precisión temporal.
         */
        builder.Property(appointment => appointment.ScheduledAt)
            .IsRequired()
            .HasColumnType("datetime2");

        /*
         * Configura el motivo de la consulta.
         * Es obligatorio y admite hasta 300 caracteres.
         */
        builder.Property(appointment => appointment.Reason)
            .IsRequired()
            .HasMaxLength(300);

        /*
         * Guarda AppointmentStatus como texto en la base de datos.
         *
         * Valores posibles:
         * - BOOKED
         * - CANCELLED
         * - ATTENDED
         * - NO_SHOW
         *
         * Esto mejora la legibilidad frente al almacenamiento
         * numérico predeterminado de los enums.
         */
        builder.Property(appointment => appointment.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);


        /*
         * Configura la fecha de creación heredada de EntityBase.
         */
        builder.Property(appointment => appointment.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        /*
         * Configura la fecha de última modificación
         * heredada de EntityBase.
         */
        builder.Property(appointment => appointment.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        /*
         * Relación:
         *
         * Doctor 1 ───── N Appointments
         *
         * Cada turno pertenece a un único médico.
         * Un médico puede estar asociado a múltiples turnos.
         *
         * WithMany() se deja vacío porque Doctor no expone
         * una colección de Appointment.
         *
         * Restrict evita la eliminación física de un médico
         * que tenga turnos asociados.
         */
        builder.HasOne(appointment => appointment.Doctor)
            .WithMany()
            .HasForeignKey(appointment => appointment.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Relación:
         *
         * Patient 1 ───── N Appointments
         *
         * Cada turno pertenece a un único paciente.
         * Un paciente puede gestionar múltiples turnos.
         *
         * WithMany() se deja vacío porque Patient no expone
         * una colección inversa de Appointment.
         *
         * Restrict protege el historial de turnos ante
         * eliminaciones físicas del paciente.
         */
        builder.HasOne(appointment => appointment.Patient)
            .WithMany()
            .HasForeignKey(appointment => appointment.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Relación:
         *
         * Availability 1 ───── N Appointments históricos
         *
         * Una disponibilidad puede estar asociada a varios
         * turnos a lo largo del tiempo, por ejemplo:
         *
         * - un turno CANCELLED;
         * - luego un nuevo turno BOOKED.
         *
         * La restricción definida más abajo garantiza que
         * solo exista un turno BOOKED activo por disponibilidad.
         *
         * Restrict evita eliminar físicamente una disponibilidad
         * que tenga turnos relacionados.
         */
        builder.HasOne(appointment => appointment.Availability)
            .WithMany()
            .HasForeignKey(appointment => appointment.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Índice único filtrado para garantizar consistencia
         * en la reserva de turnos.
         *
         * Permite múltiples registros históricos para una misma
         * disponibilidad, siempre que estén cancelados o eliminados.
         *
         * Impide que existan simultáneamente dos turnos:
         *
         * - con el mismo AvailabilityId;
         * - con estado BOOKED;
         * - y con Deleted = false.
         *
         * Esta restricción es especialmente importante ante
         * intentos concurrentes de reserva.
         */
        builder.HasIndex(
                appointment => appointment.AvailabilityId)
            .IsUnique()
            .HasFilter(
                "[Status] = 'BOOKED'");
        /*
         * Índice compuesto para optimizar la consulta de turnos
         * de un paciente ordenados o filtrados por fecha.
         */
        builder.HasIndex(appointment => new
        {
            appointment.PatientId,
            appointment.ScheduledAt
        });

        /*
         * Índice compuesto para optimizar la consulta de turnos
         * de un médico ordenados o filtrados por fecha.
         */
        builder.HasIndex(appointment => new
        {
            appointment.DoctorId,
            appointment.ScheduledAt
        });

        /*
         * Filtro global de eliminación lógica.
         *
         * EF Core agrega automáticamente la condición:
         *
         * Deleted = false
         *
         * a las consultas normales de Appointment.
         *
         * Los registros eliminados solo podrán consultarse
         * utilizando IgnoreQueryFilters().
         */
        
        //Índice para evitar la doble reserva
        builder.HasIndex(a => a.AvailabilityId)
            .IsUnique()
            .HasFilter("[Status] = 'BOOKED'");
    }
}

/*Falta respecto del modelo
CancelledAt.
AttendedAt.
RowVersion.
Relación real 0..1 entre slot y appointment.
Longitud de Reason en 300.
Unicidad absoluta de AvailabilityId.*/

/*Se agregó y no aparece en el modelo
DoctorId.
ScheduledAt.
Deleted.
query filter de soft delete.
índices por paciente/fecha y médico/fecha.
DeleteBehavior.Restrict.
ValueGeneratedNever().
reutilización histórica de una disponibilidad mediante índice filtrado.*/