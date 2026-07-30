using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

/// <summary>
/// Configura el mapeo entre la entidad Availability
/// y la tabla Availabilities de la base de datos.
/// </summary>
public class AvailabilityConfiguration
    : IEntityTypeConfiguration<Availability>
{
    /// <summary>
    /// Define las columnas, relaciones, restricciones,
    /// índices y filtros globales correspondientes a Availability.
    /// </summary>
    public void Configure(
        EntityTypeBuilder<Availability> builder)
    {
        /*
         * Asocia la entidad Availability con la tabla Availabilities.
         *
         * La implementación actual simplifica el modelo original:
         * AvailabilityRule y AvailabilitySlot se representan
         * mediante una única entidad Availability.
         */
        builder.ToTable(
            "Availabilities",
            tableBuilder =>
            {
                /*
                 * Garantiza que todo bloque horario tenga
                 * una duración válida.
                 *
                 * La hora de finalización debe ser posterior
                 * a la hora de inicio.
                 */
                tableBuilder.HasCheckConstraint(
                    "CK_Availabilities_EndTimeAfterStartTime",
                    "[end_time] > [start_time]");
            });

        // Define Id como clave primaria de la tabla.
        builder.HasKey(availability => availability.Id);

        /*
         * El identificador se genera en el dominio mediante
         * EntityBase y Guid.NewGuid().
         *
         * SQL Server no debe generar este valor automáticamente.
         */
        builder.Property(availability => availability.Id)
            .ValueGeneratedNever();

        /*
         * Configura el identificador del médico propietario
         * de la disponibilidad.
         *
         * Todo bloque horario debe pertenecer a un médico.
         */
        builder.Property(availability => availability.DoctorId)
            .IsRequired()
            .HasColumnName("doctor_id");

        /*
         * Configura la fecha concreta de la disponibilidad.
         *
         * SQL Server almacena únicamente la parte correspondiente
         * al día mediante el tipo date.
         */
        builder.Property(availability => availability.Date)
            .IsRequired()
            .HasColumnName("slot_date")
            .HasColumnType("date");

        /*
         * Configura la hora de inicio del bloque disponible.
         */
        builder.Property(availability => availability.StartTime)
            .IsRequired()
            .HasColumnName("start_time")
            .HasColumnType("time");

        /*
         * Configura la hora de finalización del bloque disponible.
         */
        builder.Property(availability => availability.EndTime)
            .IsRequired()
            .HasColumnName("end_time")
            .HasColumnType("time");

        /*
         * Indica si el bloque horario se encuentra libre.
         *
         * Toda disponibilidad nueva comienza con
         * IsAvailable = true.
         *
         * Esta propiedad simplifica el estado definido en el
         * modelo original, que distinguía AVAILABLE, BOOKED
         * y BLOCKED.
         */
        builder.Property(availability => availability.IsAvailable)
            .IsRequired()
            .HasColumnName("is_available")
            .HasColumnType("bit")
            .HasDefaultValue(true);

        /*
         * Configura la marca de eliminación lógica heredada
         * de EntityBase.
         *
         * Toda disponibilidad nueva comienza con Deleted = false.
         */
        builder.Property(availability => availability.Deleted)
            .IsRequired()
            .HasColumnName("deleted")
            .HasColumnType("bit")
            .HasDefaultValue(false);

        /*
         * Configura la fecha de creación heredada
         * de EntityBase.
         */
        builder.Property(availability => availability.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at")
            .HasColumnType("datetime2");

        /*
         * Configura la fecha de última modificación
         * heredada de EntityBase.
         */
        builder.Property(availability => availability.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");

        /*
         * Relación:
         *
         * Doctor 1 ───── N Availabilities
         *
         * Cada disponibilidad pertenece a un único médico.
         * Un médico puede definir múltiples bloques horarios.
         *
         * WithMany() se deja vacío porque Doctor no expone
         * una colección inversa de Availability.
         *
         * Restrict evita eliminar físicamente un médico
         * que tenga disponibilidades relacionadas.
         */
        builder.HasOne(availability => availability.Doctor)
            .WithMany()
            .HasForeignKey(availability => availability.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        /*
         * Evita registrar dos disponibilidades activas
         * con el mismo médico, fecha y hora de inicio.
         *
         * El filtro permite reutilizar la misma combinación
         * cuando el registro anterior fue eliminado lógicamente.
         */
        builder.HasIndex(availability => new
        {
            availability.DoctorId,
            availability.Date,
            availability.StartTime
        })
        .IsUnique()
        .HasFilter("[deleted] = 0");

        /*
         * Índice orientado a las consultas de agenda.
         *
         * Optimiza la búsqueda de disponibilidades de un médico
         * para una fecha determinada y su ordenamiento por hora.
         */
        builder.HasIndex(availability => new
        {
            availability.DoctorId,
            availability.Date,
            availability.StartTime,
            availability.EndTime
        });

        /*
         * Filtro global de eliminación lógica.
         *
         * EF Core agrega automáticamente la condición
         * Deleted = false a las consultas normales.
         *
         * Las disponibilidades eliminadas solamente podrán
         * consultarse mediante IgnoreQueryFilters().
         */
        builder.HasQueryFilter(
            availability => !availability.Deleted);
    }
}

/*
 * CONFLICTOS Y DIFERENCIAS RESPECTO DEL MODELO ORIGINAL
 *
 * 1. El modelo original separa AvailabilityRule y AvailabilitySlot.
 *    La implementación actual los unifica en Availability.
 *
 * 2. El modelo original representa el estado mediante:
 *    AVAILABLE, BOOKED y BLOCKED.
 *    La implementación utiliza IsAvailable, por lo que no puede
 *    diferenciar explícitamente entre BOOKED y BLOCKED.
 *
 * 3. Availability se relaciona directamente con Doctor porque
 *    no existe la entidad intermedia AvailabilityRule.
 *
 * 4. CreatedAt y UpdatedAt fueron agregados por herencia desde
 *    EntityBase, aunque no aparezcan en AvailabilitySlots
 *    dentro del modelo original.
 *
 * 5. El índice único evita duplicados exactos por médico,
 *    fecha y hora inicial, pero no detecta superposiciones como:
 *    10:00-10:30 y 10:15-10:45.
 *    Esa regla debe comprobarse en Application mediante una
 *    consulta previa a la creación.
 *
 * 6. El check constraint refuerza en base que EndTime sea
 *    posterior a StartTime. Esta regla también debería validarse
 *    antes en Application para devolver un error controlado.
 */