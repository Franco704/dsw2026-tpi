using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Application.Mappers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

/// <summary>
/// Gestiona los casos de uso relacionados con médicos,
/// incluyendo creación, actualización, consulta y eliminación lógica.
/// </summary>
public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    /// <summary>
    /// Inicializa el servicio con la abstracción de persistencia.
    /// </summary>
    public DoctorService(
        IPersistence persistence)
    {
        _persistence = persistence;
    }

    /// <summary>
    /// Crea un nuevo médico asociado a una especialidad existente.
    /// </summary>
    public async Task<DoctorModel.Response> Create(
        DoctorModel.Request request)
    {
        DoctorRequestValidator.Validate(request);

        var normalizedLicenseNumber =
            request.LicenseNumber.Trim();

        await EnsureLicenseNumberIsAvailableAsync(
            normalizedLicenseNumber);

        var specialty =
            await GetSpecialtyAsync(
                request.SpecialtyId);

        var doctor = new Doctor(
            request.Name,
            normalizedLicenseNumber,
            specialty);

        var createdDoctor =
            await _persistence.Add(doctor);

        return DoctorMapper.ToResponse(
            createdDoctor);
    }

    /// <summary>
    /// Actualiza los datos principales y la especialidad
    /// de un médico existente.
    /// </summary>
    public async Task<DoctorModel.Response> UpdateDoctors(
        Guid id,
        DoctorModel.Request request)
    {
        DoctorRequestValidator.Validate(request);

        var doctor =
            await _persistence.GetById<Doctor>(
                id);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        var normalizedLicenseNumber =
            request.LicenseNumber.Trim();

        /*
         * La matrícula puede conservarse durante la actualización.
         * Solamente debe rechazarse cuando pertenece a otro médico.
         */
        await EnsureLicenseNumberIsAvailableAsync(
            normalizedLicenseNumber,
            id);

        var specialty =
            await GetSpecialtyAsync(
                request.SpecialtyId);

        doctor.Update(
            request.Name,
            normalizedLicenseNumber,
            specialty);

        var updatedDoctor =
            await _persistence.Update(doctor);

        return DoctorMapper.ToResponse(
            updatedDoctor);
    }

    /// <summary>
    /// Obtiene una página de médicos activos,
    /// con filtro opcional por nombre.
    /// </summary>
    /// <summary>
    /// Obtiene una página de médicos activos,
    /// con filtro opcional por nombre.
    ///
    /// Los médicos continúan visibles cuando su especialidad
    /// fue eliminada lógicamente. En ese caso, la respuesta
    /// contiene specialty con valor null.
    /// </summary>
    public async Task<Pagination<DoctorModel.Response>> GetAll(
        int pageSize,
        int pageIndex,
        string? name = null)
    {
        DoctorRequestValidator.ValidateGetAll(
            pageSize,
            pageIndex,
            name);

        var normalizedName =
            name?.Trim();

        /*
         * La navegación Specialty no se incluye en esta consulta.
         *
         * Un Include aplicaría el query filter de Specialty y
         * ocultaría también al médico cuando su especialidad
         * estuviera eliminada.
         */
        var doctorsPage =
            await _persistence.Paginate<Doctor, string>(
                pageSize,
                pageIndex,
                doctor =>
                    doctor.IsActive &&
                    !doctor.Deleted &&
                    (
                        string.IsNullOrWhiteSpace(normalizedName) ||
                        doctor.Name.Contains(normalizedName)
                    ),
                doctor =>
                    doctor.Name);

        var doctors =
            doctorsPage.Data.ToList();

        /*
         * Se obtienen en una única consulta las especialidades
         * activas utilizadas por los médicos de esta página.
         *
         * El query filter de Specialty excluye automáticamente
         * las especialidades eliminadas.
         */
        var specialtyIds = doctors
            .Select(doctor =>
                doctor.SpecialityId)
            .Distinct()
            .ToList();

        var activeSpecialties =
            new List<Specialty>();

        if (specialtyIds.Count > 0)
        {
            activeSpecialties =
                (
                    await _persistence.GetFiltered<Specialty>(
                        specialty =>
                            specialtyIds.Contains(
                                specialty.Id))
                )?.ToList() ?? [];
        }

        var specialtiesById =
            activeSpecialties.ToDictionary(
                specialty =>
                    specialty.Id);

        /*
         * Si el diccionario no contiene la especialidad,
         * significa que fue eliminada lógicamente.
         * El mapper devolverá specialty con valor null.
         */
        var responses = doctors
            .Select(doctor =>
            {
                specialtiesById.TryGetValue(
                    doctor.SpecialityId,
                    out var specialty);

                return DoctorMapper.ToResponse(
                    doctor,
                    specialty);
            })
            .ToList();

        return new Pagination<DoctorModel.Response>(
            doctorsPage.PageSize,
            doctorsPage.PageIndex,
            doctorsPage.Total,
            responses);
    }
    /// <summary>
    /// Obtiene los bloques de disponibilidad del médico
    /// correspondientes al mes actual.
    /// 
    /// Cada elemento representa un slot real almacenado,
    /// con su propio identificador y duración de treinta minutos.
    /// </summary>
    public async Task<List<DoctorModel.AvailabilityResponse>> GetById(
        Guid id)
    {
        var doctor =
            await _persistence.GetById<Doctor>(
                id);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        var today = DateTime.Today;

        var firstDayOfMonth = new DateTime(
            today.Year,
            today.Month,
            1);

        var firstDayOfNextMonth =
            firstDayOfMonth.AddMonths(1);

        var availabilities =
            await _persistence.GetFiltered<Availability>(
                availability =>
                    availability.DoctorId == id &&
                    availability.Date >= firstDayOfMonth &&
                    availability.Date < firstDayOfNextMonth);

        if (availabilities is null)
        {
            return [];
        }

        return availabilities
            .OrderBy(
                availability =>
                    availability.Date)
            .ThenBy(
                availability =>
                    availability.StartTime)
            .Select(
                DoctorMapper.ToAvailabilityResponse)
            .ToList();
    }

    /// <summary>
    /// Elimina lógicamente un médico existente.
    /// </summary>
    public async Task DeleteDoctor(
        Guid id)
    {
        var doctor =
            await _persistence.GetById<Doctor>(
                id);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        doctor.Deactivate();

        await _persistence.Update(
            doctor);
    }

    /// <summary>
    /// Comprueba que la matrícula no pertenezca
    /// a otro médico registrado.
    /// </summary>
    private async Task EnsureLicenseNumberIsAvailableAsync(
        string licenseNumber,
        Guid? excludedDoctorId = null)
    {
        var existingDoctor =
            await _persistence.First<Doctor>(
                doctor =>
                    doctor.LicenseNumber == licenseNumber &&
                    (
                        !excludedDoctorId.HasValue ||
                        doctor.Id != excludedDoctorId.Value
                    ));

        if (existingDoctor is not null)
        {
            throw new ConflictException(
                ErrorCodes.DOCTOR_LICENSE_CONFLICT,
                nameof(ErrorCodes.DOCTOR_LICENSE_CONFLICT));
        }
    }

    /// <summary>
    /// Obtiene la especialidad solicitada o informa
    /// que no existe.
    /// </summary>
    private async Task<Specialty> GetSpecialtyAsync(
        Guid specialtyId)
    {
        var specialty =
            await _persistence.GetById<Specialty>(
                specialtyId);

        if (specialty is null)
        {
            throw new EntityNotFoundException(
                nameof(Specialty));
        }

        return specialty;
    }

}