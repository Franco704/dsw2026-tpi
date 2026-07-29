using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

/// <summary>
/// Gestiona los casos de uso relacionados con médicos,
/// incluyendo creación, actualización, consulta y desactivación.
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
        // Valida los datos recibidos en la request.
        DoctorsValidators.ValidateDoctorRequest(request);

        // Verifica que la especialidad exista.
        var speciality =
            await _persistence.GetById<Speciality>(
                request.SpecialityId);

        if (speciality is null)
        {
            throw new EntityNotFoundException(
                nameof(Speciality));
        }

        /*
         * La entidad Doctor inicializa Id, CreatedAt,
         * UpdatedAt y Deleted mediante EntityBase.
         */
        var doctor = new Doctor(
            request.Name,
            request.LicenseNumber,
            speciality);

        await _persistence.Add(doctor);

        return new DoctorModel.Response(
            doctor.Id,
            doctor.Name,
            doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(
                speciality.Id,
                speciality.Name));
    }

    /// <summary>
    /// Actualiza los datos principales y la especialidad
    /// de un médico existente.
    /// </summary>
    public async Task UpdateDoctors(
        Guid id,
        DoctorModel.Request request)
    {
        // Valida los datos recibidos en la request.
        DoctorsValidators.ValidateDoctorRequest(request);

        // Obtiene el médico que se desea modificar.
        var doctor =
            await _persistence.GetById<Doctor>(id);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
        }

        // Verifica que la nueva especialidad exista.
        var speciality =
            await _persistence.GetById<Speciality>(
                request.SpecialityId);

        if (speciality is null)
        {
            throw new EntityNotFoundException(
                nameof(Speciality));
        }

        /*
         * La entidad actualiza sus datos y UpdatedAt
         * mediante su propio comportamiento.
         */
        doctor.Update(
            request.Name,
            request.LicenseNumber,
            speciality);

        await _persistence.Update(doctor);
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
        var doctors =
            await _persistence.Paginate<Doctor, string>(
                pageSize,
                pageIndex,
                doctor =>
                    doctor.IsActive &&
                    (
                        string.IsNullOrWhiteSpace(name) ||
                        doctor.Name.Contains(name)
                    ),
                doctor => doctor.Name,
                nameof(Doctor.Speciality));

        return doctors.Map(
            doctor =>
                new DoctorModel.Response(
                    doctor.Id,
                    doctor.Name,
                    doctor.LicenseNumber,
                    new DoctorModel.SpecialityDto(
                        doctor.Speciality?.Id,
                        doctor.Speciality?.Name)));
    }

    /// <summary>
    /// Obtiene la disponibilidad mensual agrupada
    /// por día de la semana para un médico.
    /// </summary>
    public async Task<List<DoctorModel.AvailiabilityResponse>> GetById(
        Guid id)
    {
        // Verifica que el médico exista.
        var doctor =
            await _persistence.GetById<Doctor>(id);

        if (doctor is null)
        {
            throw new EntityNotFoundException(
                nameof(Doctor));
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

        // Obtiene las disponibilidades del médico durante el mes.
        var availabilities =
            await _persistence.GetFiltered<Availability>(
                availability =>
                    availability.DoctorId == id &&
                    availability.Date >= firstDayOfMonth &&
                    availability.Date <= lastDayOfMonth);

        // Devuelve una lista vacía cuando no existen horarios.
        if (availabilities is null ||
            !availabilities.Any())
        {
            return [];
        }

        /*
         * Agrupa los bloques por día de la semana y obtiene
         * el rango horario mínimo y máximo de cada día.
         */
        var schedule = availabilities
            .GroupBy(
                availability =>
                    availability.Date.DayOfWeek)
            .Select(
                group =>
                    new DoctorModel.AvailiabilityResponse(
                        Day: group.Key.ToSpanish(),
                        StartTime: group
                            .Min(availability =>
                                availability.StartTime)
                            .ToTimeString(),
                        EndTime: group
                            .Max(availability =>
                                availability.EndTime)
                            .ToTimeString()))
            .ToList();

        return schedule;
    }

    /// <summary>
    /// Desactiva lógicamente un médico existente.
    /// </summary>
    public async Task DeleteDoctor(
        Guid id)
    {
        var doctor =
            await _persistence.GetById<Doctor>(id);

        if (doctor is not null)
        {
            /*
             * Deactivate modifica IsActive, Deleted
             * y UpdatedAt dentro de la entidad.
             */
            doctor.Deactivate();

            await _persistence.Update(doctor);
        }
    }
}

/*
 * DECISIONES TOMADAS:
 *
 * - Se mantuvo la estructura original de DoctorService.    
 *
 * - No se extrajeron métodos privados ni nuevas clases.
 *
 * - EntityNotFoundException reemplaza KeyNotFoundException
 *   para mantener el tratamiento uniforme de errores.
 *
 * - No se asignan CreatedAt ni UpdatedAt desde Application,
 *   porque EntityBase y Doctor administran la auditoría.
 *
 * - DoctorsValidators continúa validando el contrato de entrada.
 *
 * - No se utiliza ErrorCodes directamente porque
 *   EntityNotFoundException centraliza ENTITY_NOTFOUND.
 *
 * - Se utiliza DateTime.Today para mantener el criterio
 *   temporal local definido en el proyecto.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - Create y UpdateDoctors no verifican explícitamente si
 *   ya existe otro médico con la misma matrícula.
 *
 * - Si la base posee un índice único para LicenseNumber,
 *   una duplicación podría producir una excepción de
 *   persistencia no traducida.
 *
 * - Antes de agregar un código nuevo, debe revisarse si alguno
 *   de los ErrorCodes existentes representa adecuadamente
 *   el conflicto.
 *
 * - DeleteDoctor no informa error si el médico no existe;
 *   conserva el comportamiento original y finaliza silenciosamente.
 *
 * - GetById realmente devuelve los horarios del médico,
 *   no los datos del médico. El nombre podría ser más específico,
 *   por ejemplo GetMonthlyAvailability.
 *
 * - GetById agrupa por DayOfWeek y toma el mínimo StartTime
 *   y máximo EndTime. Si existen intervalos separados durante
 *   un día, la respuesta puede aparentar un horario continuo.
 *
 * - GetFiltered devuelve una colección vacía y no null en la
 *   implementación actual; la comprobación de null es redundante,
 *   pero se mantiene por la firma nullable de IPersistence.
 *
 * - IsActive y Deleted representan estados relacionados.
 *   Deberá revisarse si ambos son realmente necesarios.
 */