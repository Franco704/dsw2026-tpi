using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Extensions;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Mappers;
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

    public async Task<Pagination<SpecialtyModel.Response>> GetAllSpecialtiesAsync(
        int pageSize,
        int pageIndex,
        string? name = null,
        bool? isActive = null)
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
                    (
                        string.IsNullOrWhiteSpace(normalizedName) ||
                        speciality.Name.Contains(normalizedName)
                    ) &&
                    (
                        !isActive.HasValue ||
                        speciality.IsActive == isActive.Value
                    ),
                speciality =>
                    speciality.Name);

        return page.Map(
            SpecialtyMapper.ToResponse);
    }
    public async Task<SpecialtyModel.Response> CreateSpecialtyAsync(
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
            request.Description,
            isActive: request.IsActive ?? true);

        var created =
            await _persistence.Add(speciality);
        
        await _persistence.SaveChangesAsync();


        return SpecialtyMapper.ToResponse(
            created);
    }

    public async Task<SpecialtyModel.Response> UpdateSpecialtyAsync(
        Guid id,
        SpecialtyModel.Request request)
    {
        SpecialityRequestValidator.Validate(request);

        var existing =
            await _persistence.GetRequiredById<Specialty>(id);

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
            request.Description,
            request.IsActive);

        await _persistence.Update(existing);
        
        await _persistence.SaveChangesAsync();
        
        return SpecialtyMapper.ToResponse(
            existing);
    }

    public async Task DeleteSpecialtyAsync(
        Guid id)
    {
        var speciality =
            await _persistence.GetRequiredById<Specialty>(
                id);

        var activeDoctor =
            await _persistence.First<Doctor>(
                doctor =>
                    doctor.SpecialityId == id &&
                    doctor.IsActive &&
                    !doctor.Deleted);

        if (activeDoctor is not null)
        {
            throw new ConflictException(
                ErrorCodes.SPECIALITY_HAS_ACTIVE_DOCTORS,
                nameof(ErrorCodes.SPECIALITY_HAS_ACTIVE_DOCTORS));
        }

        speciality.Deactivate();

        await _persistence.Update(
            speciality);
        await _persistence.SaveChangesAsync();

    }
}
