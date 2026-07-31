using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Valida los datos recibidos al crear o actualizar
/// la disponibilidad mensual de un médico.
///
/// Este validador controla únicamente el contenido de la request.
/// La existencia del médico y los conflictos con registros almacenados
/// son responsabilidades del servicio.
/// </summary>
public static class AvailabilityRequestValidator
{
    

    /// <summary>
    /// Representa internamente un rango que superó
    /// las validaciones individuales.
    /// </summary>
    private sealed record ValidatedSchedule(
        int Index,
        DayOfWeek Day,
        TimeSpan StartTime,
        TimeSpan EndTime);

    /// <summary>
    /// Valida el médico y los rangos horarios informados.
    /// Acumula todos los errores encontrados antes de lanzar
    /// una ValidationException.
    /// </summary>
    public static void Validate(AvailabilityModel.Request? request)
    {
        var slotDuration= AvailabilityRules.SlotDuration;
        var validation = new ValidationException();

        if (request is null)
        {
            validation.WithDetail(
                "request",
                "required");

            throw validation;
        }

        ValidateDoctorId(
            request.DoctorId,
            validation);

        ValidateDays(
            request.Days,
            validation);

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }

    /// <summary>
    /// Comprueba que la request identifique un médico.
    /// La existencia del médico se verifica posteriormente
    /// mediante persistencia.
    /// </summary>
    private static void ValidateDoctorId(
        Guid doctorId,
        ValidationException validation)
    {
        if (doctorId == Guid.Empty)
        {
            validation.WithDetail(
                "doctorId",
                "required");
        }
    }

    /// <summary>
    /// Valida cada rango informado y posteriormente comprueba
    /// que los horarios de un mismo día no se solapen.
    ///
    /// Un día puede aparecer varias veces para permitir
    /// horarios partidos.
    /// </summary>
    private static void ValidateDays(
        IReadOnlyCollection<AvailabilityModel.DaySchedule>? days,
        ValidationException validation)
    {
        if (days is null || days.Count == 0)
        {
            validation.WithDetail(
                "days",
                "required");

            return;
        }

        var validSchedules = new List<ValidatedSchedule>();
        var index = 0;

        foreach (var schedule in days)
        {
            var validatedSchedule = ValidateSchedule(
                schedule,
                index,
                validation);

            if (validatedSchedule is not null)
            {
                validSchedules.Add(validatedSchedule);
            }

            index++;
        }

        ValidateOverlappingRanges(
            validSchedules,
            validation);
    }

    /// <summary>
    /// Valida el día y el rango horario de una configuración.
    /// Devuelve un rango normalizado solamente cuando sus datos
    /// son válidos.
    /// </summary>
    private static ValidatedSchedule? ValidateSchedule(
        AvailabilityModel.DaySchedule? schedule,
        int index,
        ValidationException validation)
    {
        var fieldPrefix = $"days[{index}]";

        if (schedule is null)
        {
            validation.WithDetail(
                fieldPrefix,
                "required");

            return null;
        }

        var parsedDay = ValidateDay(
            schedule.Day,
            fieldPrefix,
            validation);

        var validTimeRange = ValidateTimeRange(
            schedule.StartTime,
            schedule.EndTime,
            fieldPrefix,
            validation);

        if (parsedDay is null || !validTimeRange)
        {
            return null;
        }

        return new ValidatedSchedule(
            index,
            parsedDay.Value,
            schedule.StartTime,
            schedule.EndTime);
    }

    /// <summary>
    /// Comprueba que el nombre corresponda a un día válido.
    /// Admite nombres en español o inglés.
    /// </summary>
    private static DayOfWeek? ValidateDay(
        string? day,
        string fieldPrefix,
        ValidationException validation)
    {
        if (string.IsNullOrWhiteSpace(day))
        {
            validation.WithDetail(
                $"{fieldPrefix}.day",
                "required");

            return null;
        }

        var parsedDay = DateTimeHelpers.ParseDay(day);

        if (parsedDay is null)
        {
            validation.WithDetail(
                $"{fieldPrefix}.day",
                "invalid_day");
        }

        return parsedDay;
    }

    /// <summary>
    /// Comprueba que el horario tenga una duración válida
    /// y pueda dividirse completamente en bloques de 30 minutos.
    /// </summary>
    private static bool ValidateTimeRange(
        TimeSpan startTime,
        TimeSpan endTime,
        string fieldPrefix,
        ValidationException validation)
    {
        if (startTime >= endTime)
        {
            validation.WithDetail(
                $"{fieldPrefix}.endTime",
                "must_be_after_start_time");

            return false;
        }

        var duration = endTime - startTime;
        var slotDuration = AvailabilityRules.SlotDuration;

        if (duration.Ticks % slotDuration.Ticks != 0)
        {
            validation.WithDetail(
                $"{fieldPrefix}.endTime",
                "range_must_be_divisible_into_30_minute_slots");

            return false;
        }

        return true;
    }

    /// <summary>
    /// Detecta solapamientos entre rangos correspondientes
    /// al mismo día.
    ///
    /// Los rangos consecutivos son válidos porque el fin de uno
    /// puede coincidir con el comienzo del siguiente.
    /// </summary>
    private static void ValidateOverlappingRanges(
        IReadOnlyCollection<ValidatedSchedule> schedules,
        ValidationException validation)
    {
        foreach (var dayGroup in schedules.GroupBy(schedule => schedule.Day))
        {
            var orderedRanges = dayGroup
                .OrderBy(schedule => schedule.StartTime)
                .ThenBy(schedule => schedule.EndTime)
                .ToList();

            for (var index = 1; index < orderedRanges.Count; index++)
            {
                var previousRange = orderedRanges[index - 1];
                var currentRange = orderedRanges[index];

                if (currentRange.StartTime < previousRange.EndTime)
                {
                    validation.WithDetail(
                        $"days[{currentRange.Index}].startTime",
                        "overlapping_time_range");
                }
            }
        }
    }
}