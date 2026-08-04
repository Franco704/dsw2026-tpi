using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

/// <summary>
/// Gestiona los casos de uso relacionados con especialidades,
/// incluyendo consulta, creación, actualización y desactivación.
/// </summary>
public class SpecialitiesService : ISpecialitiesService
{
    private readonly IPersistence _persistence;

    /// <summary>
    /// Inicializa el servicio con la abstracción de persistencia.
    /// </summary>
    public SpecialitiesService(
        IPersistence persistence)
    {
        _persistence = persistence;
    }

    /// <summary>
    /// Obtiene una página de especialidades,
    /// con filtro opcional por nombre.
    /// </summary>
    public async Task<Pagination<SpecialtyModel.Response>> GetAll(
        int pageSize,
        int pageIndex,
        string? name = null)
    {
        /*
         * La consulta no llega a persistencia hasta que
         * la paginación y el filtro sean válidos.
         */
        SpecialityRequestValidator.ValidateGetAll(
            pageSize,
            pageIndex,
            name);

        var normalizedName =
            name?.Trim();

        var page =
            await _persistence.Paginate<Specialty, string>(
                pageSize,
                pageIndex,
                speciality =>
                    string.IsNullOrWhiteSpace(normalizedName) ||
                    speciality.Name.Contains(normalizedName),
                speciality =>
                    speciality.Name);

        return page.Map(
            speciality =>
                new SpecialtyModel.Response(
                    speciality.Id,
                    speciality.Name,
                    speciality.Description));
    }
    /// <summary>
    /// Crea una nueva especialidad cuando los datos son válidos
    /// y no existe otra con el mismo nombre.
    /// </summary>
    public async Task<SpecialtyModel.Response> Create(
        SpecialtyModel.Request request)
    {
        SpecialityRequestValidator.Validate(request);

        // Comprueba que no exista otra especialidad con el mismo nombre.
        var existing =
            await _persistence.First<Specialty>(
                speciality =>
                    speciality.Name == request.Name);

        if (existing is not null)
        {
            throw new ConflictException(
                ErrorCodes.SPECIALITY_NAME_CONFLICT,
                nameof(ErrorCodes.SPECIALITY_NAME_CONFLICT));
        }

        /*
         * La entidad inicializa Id, CreatedAt,
         * UpdatedAt y Deleted mediante EntityBase.
         */
        var speciality = new Specialty(
            request.Name,
            request.Description);

        var created =
            await _persistence.Add(speciality);

        return new SpecialtyModel.Response(
            created.Id,
            created.Name,
            created.Description);
    }

    /// <summary>
    /// Actualiza los datos de una especialidad existente,
    /// verificando que el nuevo nombre no esté repetido.
    /// </summary>
    public async Task<SpecialtyModel.Response> UpdateSpecialitiy(
        Guid id,
        SpecialtyModel.Request request)
    {
        // Valida los datos requeridos para actualizar la especialidad.
        SpecialityRequestValidator.Validate(request);

        // Obtiene la especialidad que se desea modificar.
        var existing =
            await _persistence.GetById<Specialty>(id);

        if (existing is null)
        {
            throw new EntityNotFoundException(
                nameof(Specialty));
        }

        // Comprueba que el nombre no pertenezca a otra especialidad.
        var nameSpeciality = request.Name.Trim();
        
        var sameName =
            await _persistence.First<Specialty>(
                speciality =>
                    speciality.Name == nameSpeciality);

        if (sameName is not null &&
            sameName.Id != id)
        {
            throw new ConflictException(
                ErrorCodes.SPECIALITY_NAME_CONFLICT,
                nameof(ErrorCodes.SPECIALITY_NAME_CONFLICT));
        }

        /*
         * La entidad modifica sus datos y actualiza
         * UpdatedAt mediante su propio comportamiento.
         */
        existing.UpdateInfo(
            request.Name,
            request.Description);

        await _persistence.Update(existing);

        return new SpecialtyModel.Response(
            existing.Id,
            existing.Name,
            existing.Description);
    }

    /// <summary>
    /// Elimina lógicamente una especialidad existente.
    /// </summary>
    public async Task DeleteSpecialitiy(
        Guid id)
    {
        var speciality =
            await _persistence.GetById<Specialty>(
                id);

        if (speciality is null)
        {
            throw new EntityNotFoundException(
                nameof(Specialty));
        }

        speciality.Deactivate();

        await _persistence.Update(
            speciality);
    }
}
