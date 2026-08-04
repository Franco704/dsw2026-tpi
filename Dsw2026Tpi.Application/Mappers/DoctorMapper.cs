using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Mappers;

public static class DoctorMapper
{
    public static DoctorModel.Response ToResponse(
        Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(
            doctor);

        return ToResponse(
            doctor,
            doctor.Speciality);
    }

    public static DoctorModel.Response ToResponse(
        Doctor doctor,
        Specialty? specialty)
    {
        ArgumentNullException.ThrowIfNull(
            doctor);

        return new DoctorModel.Response(
            Id:
                doctor.Id,

            Name:
                doctor.Name,

            LicenseNumber:
                doctor.LicenseNumber,

            Specialty:
                specialty is null
                    ? null
                    : new DoctorModel.SpecialtyDto(
                        Id:
                            specialty.Id,

                        Name:
                            specialty.Name));
    }

    public static DoctorModel.AvailabilityResponse
        ToAvailabilityResponse(
            Availability availability)
    {
        ArgumentNullException.ThrowIfNull(
            availability);

        return new DoctorModel.AvailabilityResponse(
            Id:
                availability.Id,

            Day:
                availability.Date.DayOfWeek.ToSpanish(),

            StartTime:
                availability.StartTime.ToTimeString(),

            EndTime:
                availability.EndTime.ToTimeString());
    }
}