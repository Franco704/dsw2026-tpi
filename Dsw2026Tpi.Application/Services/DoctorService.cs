using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Extensions;
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
    private readonly TimeProvider _timeProvider;

    public DoctorService(
        IPersistence persistence,
        TimeProvider timeProvider)
    {
        _persistence = persistence;
        _timeProvider = timeProvider;
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
        
        await _persistence.SaveChangesAsync();

 
        return DoctorMapper.ToResponse(
            createdDoctor);
    }
 
    public async Task<DoctorModel.Response> UpdateDoctorAsync(
        Guid id,
        DoctorModel.Request request)
    {
        DoctorRequestValidator.Validate(request);
 
        var doctor =
            await _persistence.GetRequiredById<Doctor>(
                id);
 
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
        
        await _persistence.SaveChangesAsync();
        
        return DoctorMapper.ToResponse(
            updatedDoctor);
    }
 
    public async Task<Pagination<DoctorModel.Response>> GetAllDoctorsAsync(
        int pageSize,
        int pageIndex,
        string? name = null,
        Guid? specialtyId = null)
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
                    ) &&
                    (
                        !specialtyId.HasValue ||
                        doctor.SpecialityId == specialtyId.Value
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
            await _persistence.GetRequiredById<Doctor>(
                id);
 
        var today = _timeProvider.GetLocalNow().Date;

        var rules =
            await _persistence.GetFiltered<AvailabilityRule>(
                rule =>
                    rule.DoctorId == id &&
                    rule.Year == today.Year &&
                    rule.Month == today.Month);

        return (rules ?? [])
            .OrderBy(rule => ((int)rule.DayOfWeek + 6) % 7)
            .ThenBy(rule => rule.StartTime)
            .Select(DoctorMapper.ToAvailabilityResponse)
            .ToList();
    }
 
    public async Task DeleteDoctorAsync(
        Guid id)
    {
        var doctor =
            await _persistence.GetRequiredById<Doctor>(
                id);
 
        var now = _timeProvider.GetLocalNow().DateTime;

        var futureAppointment =
            await _persistence.First<Appointment>(
                appointment =>
                    appointment.DoctorId == id &&
                    appointment.Status == AppointmentStatus.BOOKED &&
                    appointment.ScheduledAt >= now);

        if (futureAppointment is not null)
        {
            throw new ConflictException(
                ErrorCodes.DOCTOR_HAS_FUTURE_APPOINTMENTS,
                nameof(ErrorCodes.DOCTOR_HAS_FUTURE_APPOINTMENTS));
        }

        doctor.Deactivate();
 
        await _persistence.Update(
            doctor);
        
        await _persistence.SaveChangesAsync();

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
            await _persistence.GetRequiredById<Specialty>(
                specialtyId);
 
        return specialty;
    }
    
    public async Task<List<DoctorModel.SlotResponse>> GetDoctorSlotsAsync(
        Guid doctorId,
        DateTime? from,
        DateTime? to,
        bool includeOccupied = false)
    {
        DoctorRequestValidator.ValidateSlotRange(from, to);

        var doctor = await _persistence.GetById<Doctor>(doctorId);
        
        if (doctor is null || !doctor.IsBookable)
        {
            throw new EntityNotFoundException(nameof(Doctor));
        }

        var today = _timeProvider.GetLocalNow().Date;
        var startDate = from!.Value.Date < today ? today : from.Value.Date;
        var endDate = to!.Value.Date;

        var slots = await _persistence.GetFiltered<Availability>(
            availability =>
                availability.DoctorId == doctorId &&
                !availability.Deleted &&
                availability.Date >= startDate &&
                availability.Date <= endDate);

        var now = _timeProvider.GetLocalNow().DateTime;

        // Sin includeOccupied se mantiene la respuesta de siempre (solo libres).
        // Con includeOccupied también vuelven los reservados, marcados como OCCUPIED,
        // para que el paciente los vea en gris y no pueda elegirlos.
        return (slots ?? [])
            .Where(availability => availability.StartsAt > now)
            .Where(availability => includeOccupied || availability.IsAvailable)
            .OrderBy(availability => availability.Date)
            .ThenBy(availability => availability.StartTime)
            .Select(DoctorMapper.ToSlotResponse)
            .ToList();
    }
 
}
 