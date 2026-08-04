using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class SpecialitiesService : ISpecialitiesService
{
    private readonly IPersistence _persistence;

    public SpecialitiesService(
        IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<SpecialtyModel.Response>> GetAll(
        int pageSize,
        int pageIndex,
        string? name = null)
    {
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
    public async Task<SpecialtyModel.Response> Create(
        SpecialtyModel.Request request)
    {
        SpecialityRequestValidator.Validate(request);

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

    public async Task<SpecialtyModel.Response> UpdateSpecialitiy(
        Guid id,
        SpecialtyModel.Request request)
    {
        SpecialityRequestValidator.Validate(request);

        var existing =
            await _persistence.GetById<Specialty>(id);

        if (existing is null)
        {
            throw new EntityNotFoundException(
                nameof(Specialty));
        }

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

        existing.UpdateInfo(
            request.Name,
            request.Description);

        await _persistence.Update(existing);

        return new SpecialtyModel.Response(
            existing.Id,
            existing.Name,
            existing.Description);
    }

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
