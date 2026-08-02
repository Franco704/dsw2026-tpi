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

        var doctors =
            await _persistence.Paginate<Doctor, string>(
                pageSize,
                pageIndex,
                doctor =>
                    doctor.IsActive &&
                    (
                        string.IsNullOrWhiteSpace(normalizedName) ||
                        doctor.Name.Contains(normalizedName)
                    ),
                doctor => doctor.Name,
                nameof(Doctor.Speciality));

        return doctors.Map(
            DoctorMapper.ToResponse);
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