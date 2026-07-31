using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Application.Generators;

/// <summary>
/// Genera bloques concretos de disponibilidad a partir
/// de las reglas semanales configuradas para un médico.
///
/// Esta clase no consulta ni modifica la base de datos.
/// Las reglas recibidas deben haber sido comprobadas previamente
/// mediante AvailabilityRequestValidator.
/// </summary>
public static class AvailabilitySlotGenerator
{
    /// <summary>
    /// Genera las disponibilidades comprendidas entre el momento actual
    /// y el último día del mismo mes.
    ///
    /// No genera bloques para días anteriores, meses posteriores
    /// ni horarios cuyo comienzo ya haya pasado.
    /// </summary>
    /// <param name="doctorId">
    /// Identificador del médico propietario de las disponibilidades.
    /// </param>
    /// <param name="schedules">
    /// Reglas semanales previamente validadas.
    /// Puede contener varios rangos para un mismo día.
    /// </param>
    /// <param name="currentDateTime">
    /// Fecha y hora desde las cuales comienza la generación.
    /// Se recibe como parámetro para no depender directamente
    /// del reloj del sistema dentro del generador.
    /// </param>
    public static IReadOnlyCollection<Availability> Generate(
        Guid doctorId,
        IReadOnlyCollection<AvailabilityModel.DaySchedule> schedules,
        DateTime currentDateTime)
    {
        ArgumentNullException.ThrowIfNull(schedules);

        var today = currentDateTime.Date;

        var lastDayOfMonth = new DateTime(
            today.Year,
            today.Month,
            DateTime.DaysInMonth(
                today.Year,
                today.Month));

        var generatedAvailabilities =
            new List<Availability>();

        foreach (var schedule in schedules)
        {
            /*
             * El nombre del día ya fue validado.
             * La conversión es necesaria para localizar las fechas
             * correspondientes dentro del mes actual.
             */
            var targetDayOfWeek = DateTimeHelpers.ParseDay(
                schedule.Day);

            if (targetDayOfWeek is null)
            {
                throw new InvalidOperationException(
                    $"El día '{schedule.Day}' no fue validado correctamente.");
            }

            GenerateScheduleAvailabilities(
                doctorId,
                schedule,
                targetDayOfWeek.Value,
                today,
                lastDayOfMonth,
                currentDateTime,
                generatedAvailabilities);
        }

        /*
         * Las reglas pueden llegar en cualquier orden.
         * Se devuelve un calendario ordenado por fecha y hora.
         */
        return generatedAvailabilities
            .OrderBy(availability =>
                availability.Date)
            .ThenBy(availability =>
                availability.StartTime)
            .ToList();
    }

    /// <summary>
    /// Busca dentro del período las fechas que coinciden
    /// con el día semanal configurado.
    /// </summary>
    private static void GenerateScheduleAvailabilities(
        Guid doctorId,
        AvailabilityModel.DaySchedule schedule,
        DayOfWeek targetDayOfWeek,
        DateTime firstDate,
        DateTime lastDate,
        DateTime currentDateTime,
        ICollection<Availability> generatedAvailabilities)
    {
        for (
            var date = firstDate;
            date <= lastDate;
            date = date.AddDays(1))
        {
            if (date.DayOfWeek != targetDayOfWeek)
            {
                continue;
            }

            GenerateDailySlots(
                doctorId,
                date,
                schedule.StartTime,
                schedule.EndTime,
                currentDateTime,
                generatedAvailabilities);
        }
    }

    /// <summary>
    /// Divide un rango diario en bloques consecutivos
    /// de treinta minutos.
    ///
    /// Si la fecha corresponde al día actual, descarta los bloques
    /// cuyo horario de inicio sea anterior al momento recibido.
    /// </summary>
    private static void GenerateDailySlots(
        Guid doctorId,
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime,
        DateTime currentDateTime,
        ICollection<Availability> generatedAvailabilities)
    {
        var slotDuration =
            AvailabilityRules.SlotDuration;

        for (
            var currentStart = startTime;
            currentStart < endTime;
            currentStart = currentStart.Add(slotDuration))
        {
            var currentEnd = currentStart.Add(
                slotDuration);

            var slotStartDateTime = date.Date.Add(
                currentStart);

            /*
             * Se controla el inicio, no el final.
             * Un bloque parcialmente transcurrido tampoco puede
             * publicarse como una disponibilidad nueva.
             */
            if (slotStartDateTime < currentDateTime)
            {
                continue;
            }

            generatedAvailabilities.Add(
                new Availability(
                    doctorId,
                    date,
                    currentStart,
                    currentEnd));
        }
    }
}