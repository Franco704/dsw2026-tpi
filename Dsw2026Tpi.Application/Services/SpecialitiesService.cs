using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
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