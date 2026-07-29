using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

/// <summary>
/// Gestiona la creación y actualización de las disponibilidades
/// mensuales de los médicos.
/// </summary>
public class AvailabilityService : IAvailabilitiesService
{
    private readonly IPersistence _persistence;

    /// <summary>
    /// Inicializa el servicio con la abstracción de persistencia.
    /// </summary>
    public AvailabilityService(
        IPersistence persistence)
    {
        _persistence = persistence;
    }

    /// <summary>
    /// Crea las disponibilidades del médico para el mes actual.
    /// </summary>
    public async Task<IEnumerable<AvailabilityModel.Response>> Create(
        AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(
            request,
            false);
    }

    /// <summary>
    /// Actualiza las disponibilidades del médico para el mes actual.
    /// </summary>
    public async Task<IEnumerable<AvailabilityModel.Response>>
        UpdateAvailability(
            AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(
            request,
            true);
    }

    /// <summary>
    /// Ejecuta el proceso común de creación o actualización
    /// de disponibilidades.
    /// </summary>
    private async Task<IEnumerable<AvailabilityModel.Response>>
        ProcessAvailabilitiesAsync(
            AvailabilityModel.Request request,
            bool isUpdate)
    {
        // Verifica que el médico exista.
        var doctor = await _persistence.GetById<Doctor>(
            request.DoctorId);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        // Valida que se haya informado al menos un día.
        if (request.Days is null ||
            !request.Days.Any())
        {
            var validation = new ValidationException();

            validation.WithDetail(
                nameof(request.Days),
                "Debe especificarse al menos un día con sus horarios.");

            throw validation;
        }

        // Determina los límites del mes actual.
        var today = DateTime.Today;

        var firstDayOfMonth = new DateTime(
            today.Year,
            today.Month,
            1);

        var lastDayOfMonth = new DateTime(
            today.Year,
            today.Month,
            DateTime.DaysInMonth(
                today.Year,
                today.Month));

        // Obtiene las disponibilidades existentes del médico.
        var existingAvailabilities =
            (await _persistence.GetFiltered<Availability>(
                availability =>
                    availability.DoctorId == request.DoctorId &&
                    availability.Date >= firstDayOfMonth &&
                    availability.Date <= lastDayOfMonth))
            ?.ToList()
            ?? [];

        /*
         * En una actualización, elimina lógicamente los bloques
         * que todavía se encuentran disponibles.
         */
        if (isUpdate)
        {
            foreach (var availability in existingAvailabilities)
            {
                if (availability.IsAvailable)
                {
                    availability.Delete();

                    await _persistence.Update(
                        availability);
                }
            }

            /*
             * Conserva en memoria únicamente los bloques ocupados,
             * ya que no deben reemplazarse.
             */
            existingAvailabilities = existingAvailabilities
                .Where(availability =>
                    !availability.IsAvailable)
                .ToList();
        }

        var newAvailabilities =
            new List<Availability>();

        foreach (var dayRequest in request.Days)
        {
            // Valida el rango horario informado.
            if (dayRequest.StartTime >= dayRequest.EndTime)
            {
                var validation = new ValidationException();

                validation.WithDetail(
                    nameof(dayRequest.EndTime),
                    "La hora de finalización debe ser posterior " +
                    "a la hora de inicio.");

                throw validation;
            }

            // Convierte el nombre del día a DayOfWeek.
            var targetDayOfWeek =
                DateTimeHelpers.ParseDay(
                    dayRequest.Day);

            if (targetDayOfWeek is null)
            {
                var validation = new ValidationException();

                validation.WithDetail(
                    nameof(dayRequest.Day),
                    $"El día '{dayRequest.Day}' no es válido.");

                throw validation;
            }

            /*
             * Recorre todas las fechas del mes y procesa
             * únicamente las que coinciden con el día solicitado.
             */
            for (
                var date = firstDayOfMonth;
                date <= lastDayOfMonth;
                date = date.AddDays(1))
            {
                if (date < today ||
                    date.DayOfWeek != targetDayOfWeek)
                {
                    continue;
                }

                var currentStart =
                    dayRequest.StartTime;

                /*
                 * Divide el rango horario en bloques
                 * de treinta minutos.
                 */
                while (currentStart < dayRequest.EndTime)
                {
                    var currentEnd = currentStart.Add(
                        TimeSpan.FromMinutes(30));

                    if (currentEnd > dayRequest.EndTime)
                    {
                        currentEnd =
                            dayRequest.EndTime;
                    }

                    // Comprueba que el bloque no se superponga.
                    var isOccupied =
                        existingAvailabilities.Any(
                            availability =>
                                availability.Date == date &&
                                currentStart <
                                    availability.EndTime &&
                                currentEnd >
                                    availability.StartTime);

                    if (isOccupied)
                    {
                        throw new ConflictException(
                            ErrorCodes.AVAILABILITY_CONFLICT,
                            nameof(ErrorCodes.AVAILABILITY_CONFLICT));
                    }

                    // Crea el nuevo bloque disponible.
                    var availability =
                        new Availability(
                            request.DoctorId,
                            date,
                            currentStart,
                            currentEnd);

                    newAvailabilities.Add(
                        availability);

                    currentStart = currentEnd;
                }
            }
        }

        // Persiste las disponibilidades generadas.
        foreach (var availability in newAvailabilities)
        {
            await _persistence.Add(
                availability);
        }

        // Convierte las entidades en DTOs de respuesta.
        return newAvailabilities.Select(
            availability =>
                new AvailabilityModel.Response(
                    availability.Id,
                    availability.DoctorId,
                    availability.Date,
                    availability.StartTime,
                    availability.EndTime));
    }
}

/*
 * DECISIONES TOMADAS:
 *
 * - Se mantuvo la estructura original del servicio.
 * - No se extrajeron métodos ni nuevas clases auxiliares.
 * - EntityNotFoundException reemplaza KeyNotFoundException.
 * - ValidationException reemplaza ArgumentException.
 * - ConflictException reemplaza InvalidOperationException.
 * - El conflicto utiliza ErrorCodes.AVAILABILITY_CONFLICT.
 * - La entidad Availability sigue validando internamente
 *   que EndTime sea posterior a StartTime.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - Durante una actualización se eliminan disponibilidades
 *   antes de terminar de validar toda la request.
 *
 * - Add y Update ejecutan SaveChangesAsync en cada llamada,
 *   por lo que se realizan múltiples operaciones en la base.
 *
 * - No existe una transacción que garantice que toda la
 *   actualización se confirme o revierta como una unidad.
 *
 * - El control de solapamiento solo compara contra los bloques
 *   previamente existentes, no contra los nuevos bloques ya
 *   generados dentro de la misma request.
 *
 * - Si la request contiene dos reglas superpuestas entre sí,
 *   podrían generarse bloques duplicados o conflictivos.
 *
 * - La duración de treinta minutos está escrita directamente
 *   en el método y podría centralizarse en una constante.
 *
 * - El método ProcessAvailabilitiesAsync concentra validación,
 *   generación, actualización, persistencia y mapeo. Por ahora
 *   se mantiene para no alterar significativamente el código
 *   realizado por el equipo.
 */