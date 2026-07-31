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
    /// Genera las disponibilidades comprendidas entre la fecha actual
    /// y el último día de ese mismo mes.
    ///
    /// Los días anteriores a la fecha recibida y los meses posteriores
    /// no forman parte de la generación.
    /// </summary>
    /// <param name="doctorId">
    /// Identificador del médico propietario de las disponibilidades.
    /// </param>
    /// <param name="schedules">
    /// Reglas semanales previamente validadas.
    /// Puede contener varios rangos para un mismo día.
    /// </param>
    /// <param name="currentDate">
    /// Fecha desde la cual comienza la generación.
    /// Recibirla como parámetro permite probar el generador
    /// sin depender directamente del reloj del sistema.
    /// </param>
    /// <param name="nonWorkingDates">
    /// Fechas que deben excluirse de la generación.
    /// Por el momento puede omitirse. Posteriormente serán
    /// obtenidas desde el archivo JSON de feriados nacionales.
    /// </param>
    public static IReadOnlyCollection<Availability> Generate(
        Guid doctorId,
        IReadOnlyCollection<AvailabilityModel.DaySchedule> schedules,
        DateTime currentDate,
        IReadOnlySet<DateOnly>? nonWorkingDates = null)
    {
        ArgumentNullException.ThrowIfNull(schedules);

        var today = currentDate.Date;

        var lastDayOfMonth = new DateTime(
            today.Year,
            today.Month,
            DateTime.DaysInMonth(
                today.Year,
                today.Month));

        var generatedAvailabilities = new List<Availability>();
        var slotDuration= AvailabilityRules.SlotDuration;
        foreach (var schedule in schedules)
        {
            /*
             * La validez del nombre ya fue comprobada por el validador.
             * Aquí se convierte nuevamente porque el generador necesita
             * el DayOfWeek para encontrar las fechas correspondientes.
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
                nonWorkingDates,
                generatedAvailabilities);
        }

        /*
         * Las reglas pueden llegar en cualquier orden.
         * Se ordena el resultado para devolver un calendario predecible.
         */
        return generatedAvailabilities
            .OrderBy(availability => availability.Date)
            .ThenBy(availability => availability.StartTime)
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
        IReadOnlySet<DateOnly>? nonWorkingDates,
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

            if (IsNonWorkingDate(
                date,
                nonWorkingDates))
            {
                continue;
            }

            GenerateDailySlots(
                doctorId,
                date,
                schedule.StartTime,
                schedule.EndTime,
                generatedAvailabilities);
        }
    }

    /// <summary>
    /// Divide un rango diario en bloques consecutivos
    /// de treinta minutos.
    /// </summary>
    private static void GenerateDailySlots(
        Guid doctorId,
        DateTime date,
        TimeSpan startTime,
        TimeSpan endTime,
        ICollection<Availability> generatedAvailabilities)
    {
        var slotDuration = AvailabilityRules.SlotDuration;

        for (
            var currentStart = startTime;
            currentStart < endTime;
            currentStart = currentStart.Add(slotDuration))
        {
            var currentEnd = currentStart.Add(
                slotDuration);

            generatedAvailabilities.Add(
                new Availability(
                    doctorId,
                    date,
                    currentStart,
                    currentEnd));
        }
    }
     
    /// <summary>
    /// Indica si una fecha debe excluirse por ser feriado
    /// o día no laborable.
    ///
    /// En una etapa posterior, Application recibirá estas fechas
    /// desde un componente encargado de leer el JSON de feriados
    /// nacionales. El generador solo utiliza la colección resultante
    /// y no conoce el origen de los datos.
    /// </summary>
    private static bool IsNonWorkingDate(
        DateTime date,
        IReadOnlySet<DateOnly>? nonWorkingDates)
    {
        if (nonWorkingDates is null ||
            nonWorkingDates.Count == 0)
        {
            return false;
        }

        return nonWorkingDates.Contains(
            DateOnly.FromDateTime(date));
    }
}