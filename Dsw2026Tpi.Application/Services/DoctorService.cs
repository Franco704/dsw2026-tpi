using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Validators;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<DoctorModel.Response> Create(DoctorModel.Request request)
    {
        DoctorsValidators.ValidateDoctorRequest(request);
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);
        if (speciality == null)
            throw new EntityNotFoundException(nameof(Speciality));
        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality)
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _persistence.Add(doctor);
        return new DoctorModel.Response(doctor.Id, doctor.Name, doctor.LicenseNumber, new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));
    }

    public async Task UpdateDoctors(Guid id, DoctorModel.Request request)
    {
       DoctorsValidators.ValidateDoctorRequest(request);
       var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null)
        {
            throw new EntityNotFoundException(nameof(Doctor));
        }
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId);
        if (speciality == null)
            throw new EntityNotFoundException(nameof(Speciality));

        doctor.Update(request.Name, request.LicenseNumber, speciality);
        await _persistence.Update(doctor);
    }
    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        
        var doctors = await _persistence.Paginate<Doctor, string>(pageSize, pageIndex, 
            d => d.IsActive && (string.IsNullOrWhiteSpace(name) || d.Name.Contains(name)), 
            x => x.Name, nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

    public async Task<List<DoctorModel.AvailiabilityResponse>> GetById(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor == null)
            throw new KeyNotFoundException($"El médico con ID {id} no fue encontrado.");

        var today = DateTime.UtcNow.Date;
        var fDay = new DateTime(today.Year, today.Month, 1);
        var lDay = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        
        var availabilities = await _persistence.GetFiltered<Availability>(a =>
            a.DoctorId == id &&
            a.Date >= fDay &&
            a.Date <= lDay);

        if (availabilities == null || !availabilities.Any())
        {
            return new List<DoctorModel.AvailiabilityResponse>();
        }
        var sched = availabilities
            .GroupBy(a => a.Date.DayOfWeek)
            .Select(g => new DoctorModel.AvailiabilityResponse(
                Day: g.Key.ToSpanish(),
                StartTime: g.Min(a => a.StartTime).ToTimeString(),
                EndTime: g.Max(a => a.EndTime).ToTimeString()))
            .ToList();
        return sched;
    }

    public async Task DeleteDoctor(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id);
        if (doctor != null)
        {
            doctor.Deactivate();
            await _persistence.Update(doctor);
        }
    }
}
