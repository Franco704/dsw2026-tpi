using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Application.Mappers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;
    public DoctorService(
        IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<DoctorModel.Response> CreateDoctorAsync(
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

    public async Task<DoctorModel.Response> UpdateDoctorAsync(
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

    public async Task<Pagination<DoctorModel.Response>> GetAllDoctorsAsync(
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
    public async Task<List<DoctorModel.AvailabilityResponse>> GetDoctorAvailabilitiesAsync(
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

    public async Task DeleteDoctorAsync(
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