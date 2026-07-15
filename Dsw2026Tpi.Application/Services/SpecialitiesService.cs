using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialitiesService : ISpecialitiesService
{
    private readonly IPersistence _persistence;
    
    public SpecialitiesService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<SpecialityModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        var page = await _persistence.Paginate<Speciality, string>(
            pageSize,
            pageIndex,
            s => string.IsNullOrWhiteSpace(name) || s.Name.Contains(name),
            s => s.Name);

        return page.Map(s => new SpecialityModel.Response(s.Id, s.Name, s.Description));
    }

    public async Task<SpecialityModel.Response> Create(SpecialityModel.Request request)
    {
        var validation = new ValidationException();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length is < 3 or > 100)
            validation.WithDetail(nameof(request.Name), "El nombre debe tener entre 3 y 100 caracteres");

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length is < 10 or > 100)
            validation.WithDetail(nameof(request.Description), "La descripción debe tener entre 10 y 100 caracteres");

        if (validation.Error.Details.Any()) throw validation;

        var existing = await _persistence.First<Speciality>(s => s.Name == request.Name);
        if (existing is not null)
            throw new ConflictException(nameof(ErrorCodes.SPECIALITY_NAME_CONFLICT), ErrorCodes.SPECIALITY_NAME_CONFLICT);

        var speciality = new Speciality(request.Name, request.Description)
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _persistence.Add(speciality);

        return new SpecialityModel.Response(created.Id, created.Name, created.Description);
    }

    public async Task UpdateSpecialitiy(Guid id, SpecialityModel.Request request)
    {
        if (request.Name == null || request.Description == null)
            throw new Exception("Nombre o Descripcion no pueden estar vacios");
       var speciality = await _persistence.GetById<Speciality>(id);
       if (speciality != null)
       {
           var existenombre = await _persistence.First<Speciality>(s => s.Name == request.Name);
           if (existenombre.Id != id) throw new Exception("Ya existe una especialidad con ese nombre");
           speciality.UpdateInfo(request.Name, request.Description);
           await _persistence.Update(speciality);
       }
    }

    public async Task DeleteSpecialitiy(Guid id)
    {
        var speciality = await _persistence.GetById<Speciality>(id);

        if (speciality != null)
        {
            speciality.Deactivate();
            await _persistence.Update(speciality);
        }
    }
}