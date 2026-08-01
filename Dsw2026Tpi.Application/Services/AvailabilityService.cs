using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Generators;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
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
    private readonly IFeriadoProvider _feriado;
    /// <summary>
    /// Inicializa el servicio con la abstracción de persistencia.
    /// </summary>
    public AvailabilityService(
        IPersistence persistence, IFeriadoProvider feriadoProvider)
    {
        _persistence = persistence;
        _feriado = feriadoProvider;
    }

    /// <summary>
    /// Crea las disponibilidades del médico desde la fecha actual
    /// hasta el último día del mismo mes.
    /// </summary>
    public async Task<IEnumerable<AvailabilityModel.Response>> Create(
        AvailabilityModel.Request request)
    {
        return await ProcessAvailabilitiesAsync(
            request,
            false);
    }

    /// <summary>
    /// Reemplaza las disponibilidades libres del médico desde
    /// la fecha actual hasta el último día del mismo mes.
    ///
    /// Las disponibilidades ocupadas se conservan y no pueden
    /// ser reemplazadas.
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
    /// Ejecuta el flujo compartido por la creación
    /// y la actualización de disponibilidades.
    /// </summary>
    private async Task<IEnumerable<AvailabilityModel.Response>>
        ProcessAvailabilitiesAsync(
            AvailabilityModel.Request request,
            bool isUpdate)
    {
        /*
         * La request se valida completamente antes de consultar
         * o modificar información almacenada.
         */
        AvailabilityRequestValidator.Validate(
            request);

        await EnsureDoctorExistsAsync(
            request.DoctorId);

        var currentDateTime = DateTime.Now;
        var today = currentDateTime.Date;

        /*
         * El generador se ocupa de calcular fechas y dividir
         * los rangos validados en bloques de treinta minutos.
         *
         * Por el momento no se proporciona la colección
         * de feriados nacionales.
         */
        var generatedAvailabilities =
            AvailabilitySlotGenerator.Generate(
                    request.DoctorId,
                    request.Days,
                    currentDateTime)
                    .Where(a => !_feriado.EsFeriado(a.Date))
                .ToList();

        var existingAvailabilities =
            await GetExistingAvailabilitiesAsync(
                request.DoctorId,
                today);

        /*
         * Durante una actualización, las disponibilidades libres
         * serán reemplazadas. Por eso solamente los bloques ocupados
         * deben impedir la generación del nuevo calendario.
         *
         * Durante una creación, cualquier bloque existente
         * representa un conflicto.
         */
        var protectedAvailabilities = isUpdate
            ? existingAvailabilities
                .Where(availability =>
                    !availability.IsAvailable)
                .ToList()
            : existingAvailabilities;

        EnsureNoStoredConflicts(
            generatedAvailabilities,
            protectedAvailabilities);

        /*
         * La eliminación se realiza después de validar y generar
         * todo el nuevo calendario para evitar modificaciones
         * ante una request inválida o un conflicto conocido.
         */
        if (isUpdate)
        {
            await SoftDeleteAvailableSlotsAsync(
                existingAvailabilities);
        }

        await PersistGeneratedAvailabilitiesAsync(
            generatedAvailabilities);

        return MapResponses(
            generatedAvailabilities);
    }

    /// <summary>
    /// Comprueba que el médico exista y se encuentre visible
    /// para las consultas normales de persistencia.
    /// </summary>
    private async Task EnsureDoctorExistsAsync(
        Guid doctorId)
    {
        var doctor = await _persistence.GetById<Doctor>(
            doctorId);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }
    }

    /// <summary>
    /// Obtiene las disponibilidades activas comprendidas entre
    /// la fecha actual y el comienzo del mes siguiente.
    ///
    /// No se incluyen fechas anteriores del mismo mes porque
    /// ya no pueden ser modificadas ni generadas nuevamente.
    /// </summary>
    private async Task<List<Availability>>
        GetExistingAvailabilitiesAsync(
            Guid doctorId,
            DateTime today)
    {
        var firstDayOfNextMonth = new DateTime(
                today.Year,
                today.Month,
                1)
            .AddMonths(1);

        var availabilities =
            await _persistence.GetFiltered<Availability>(
                availability =>
                    availability.DoctorId == doctorId &&
                    availability.Date >= today &&
                    availability.Date < firstDayOfNextMonth);

        return availabilities?.ToList() ?? [];
    }

    /// <summary>
    /// Verifica que los bloques generados no se solapen
    /// con disponibilidades almacenadas que deben conservarse.
    /// </summary>
    private static void EnsureNoStoredConflicts(
        IReadOnlyCollection<Availability> generatedAvailabilities,
        IReadOnlyCollection<Availability> storedAvailabilities)
    {
        var hasConflict = generatedAvailabilities.Any(
            generated =>
                storedAvailabilities.Any(
                    stored =>
                        generated.Date == stored.Date &&
                        generated.StartTime < stored.EndTime &&
                        generated.EndTime > stored.StartTime));

        if (hasConflict)
        {
            throw new ConflictException(
                ErrorCodes.AVAILABILITY_CONFLICT,
                nameof(ErrorCodes.AVAILABILITY_CONFLICT));
        }
    }

    /// <summary>
    /// Elimina lógicamente las disponibilidades que continúan libres.
    ///
    /// Los bloques ocupados se conservan porque pueden estar
    /// relacionados con turnos existentes.
    /// </summary>
    private async Task SoftDeleteAvailableSlotsAsync(
        IEnumerable<Availability> existingAvailabilities)
    {
        foreach (var availability in existingAvailabilities)
        {
            if (!availability.IsAvailable)
            {
                continue;
            }

            availability.Delete();

            await _persistence.Update(
                availability);
        }
    }

    /// <summary>
    /// Persiste los bloques generados para el nuevo calendario.
    /// </summary>
    private async Task PersistGeneratedAvailabilitiesAsync(
        IEnumerable<Availability> generatedAvailabilities)
    {
        foreach (var availability in generatedAvailabilities)
        {
            await _persistence.Add(
                availability);
        }
    }

    /// <summary>
    /// Convierte las entidades generadas al contrato
    /// de respuesta de la API.
    /// </summary>
    private static IEnumerable<AvailabilityModel.Response>
        MapResponses(
            IEnumerable<Availability> availabilities)
    {
        return availabilities.Select(
            availability =>
                new AvailabilityModel.Response(
                    availability.Id,
                    availability.DoctorId,
                    availability.Date,
                    availability.StartTime,
                    availability.EndTime));
    }
}