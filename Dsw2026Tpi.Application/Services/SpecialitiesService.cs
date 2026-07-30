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
    public async Task<Pagination<SpecialityModel.Response>> GetAll(
        int pageSize,
        int pageIndex,
        string? name = null)
    {
        var page =
            await _persistence.Paginate<Speciality, string>(
                pageSize,
                pageIndex,
                speciality =>
                    string.IsNullOrWhiteSpace(name) ||
                    speciality.Name.Contains(name),
                speciality => speciality.Name);

        return page.Map(
            speciality =>
                new SpecialityModel.Response(
                    speciality.Id,
                    speciality.Name,
                    speciality.Description));
    }

    /// <summary>
    /// Crea una nueva especialidad cuando los datos son válidos
    /// y no existe otra con el mismo nombre.
    /// </summary>
    public async Task<SpecialityModel.Response> Create(
        SpecialityModel.Request request)
    {
        SpecialityRequestValidator.Validate(request);

        // Comprueba que no exista otra especialidad con el mismo nombre.
        var existing =
            await _persistence.First<Speciality>(
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
        var speciality = new Speciality(
            request.Name,
            request.Description);

        var created =
            await _persistence.Add(speciality);

        return new SpecialityModel.Response(
            created.Id,
            created.Name,
            created.Description);
    }

    /// <summary>
    /// Actualiza los datos de una especialidad existente,
    /// verificando que el nuevo nombre no esté repetido.
    /// </summary>
    public async Task<SpecialityModel.Response> UpdateSpecialitiy(
        Guid id,
        SpecialityModel.Request request)
    {
        // Valida los datos requeridos para actualizar la especialidad.
        SpecialityRequestValidator.Validate(request);

        // Obtiene la especialidad que se desea modificar.
        var existing =
            await _persistence.GetById<Speciality>(id);

        if (existing is null)
        {
            throw new EntityNotFoundException(
                nameof(Speciality));
        }

        // Comprueba que el nombre no pertenezca a otra especialidad.
        var sameName =
            await _persistence.First<Speciality>(
                speciality =>
                    speciality.Name == request.Name);

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

        return new SpecialityModel.Response(
            existing.Id,
            existing.Name,
            existing.Description);
    }

    /// <summary>
    /// Desactiva lógicamente una especialidad existente.
    /// </summary>
    public async Task DeleteSpecialitiy(
        Guid id)
    {
        var speciality =
            await _persistence.GetById<Speciality>(id);

        if (speciality is not null)
        {
            /*
             * Deactivate modifica Deleted y UpdatedAt
             * dentro de la entidad.
             */
            speciality.Deactivate();

            await _persistence.Update(speciality);
        }
    }
}

/*
 * DECISIONES TOMADAS:
 *
 * - Se mantuvo la estructura original de SpecialitiesService.
 *
 * - No se extrajeron métodos privados ni validadores nuevos.
 *
 * - ValidationException continúa acumulando los errores
 *   de nombre y descripción mediante WithDetail.
 *
 * - EntityNotFoundException reemplaza KeyNotFoundException
 *   para utilizar el código centralizado ENTITY_NOTFOUND.
 *
 * - ConflictException recibe primero el mensaje y luego
 *   el código de error.
 *
 * - SPECIALITY_NAME_CONFLICT se utiliza tanto en Create
 *   como en UpdateSpecialitiy.
 *
 * - No se asignan CreatedAt ni UpdatedAt desde Application,
 *   porque la entidad administra su auditoría.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - La validación de nombre y descripción está repetida en
 *   Create y UpdateSpecialitiy. Podría centralizarse más adelante
 *   en un validador específico, pero se mantiene para respetar
 *   la estructura original.
 *
 * - La comparación de nombres es exacta y sensible al criterio
 *   de comparación de la base. Valores como "Cardiología" y
 *   " cardiología " podrían requerir normalización.
 *
 * - Aunque se valida previamente la duplicación, la base debe
 *   conservar un índice único para proteger la concurrencia.
 *
 * - DeleteSpecialitiy finaliza silenciosamente cuando la entidad
 *   no existe. Se mantiene el comportamiento original.
 *
 * - Los nombres UpdateSpecialitiy y DeleteSpecialitiy contienen
 *   un error ortográfico. El término correcto sería Speciality,
 *   por ejemplo UpdateSpeciality y DeleteSpeciality.
 *
 * - Cambiar esos nombres implica actualizar la interfaz,
 *   el controller y cualquier llamada existente, por lo que
 *   no se modificaron en esta revisión.
 */