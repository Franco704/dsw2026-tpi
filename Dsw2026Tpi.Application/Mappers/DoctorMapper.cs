using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Mappers;

/// <summary>
/// Centraliza la conversión de médicos y sus disponibilidades
/// a los contratos públicos de respuesta.
/// </summary>
public static class DoctorMapper
{
    /// <summary>
    /// Convierte un médico al response contractual,
    /// incluyendo su especialidad.
    /// </summary>
    public static DoctorModel.Response ToResponse(
        Doctor doctor)
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
                new DoctorModel.SpecialtyDto(
                    Id:
                        doctor.Speciality?.Id,

                    Name:
                        doctor.Speciality?.Name));
    }

    /// <summary>
    /// Convierte un slot de disponibilidad al formato
    /// expuesto por la consulta de médicos.
    /// </summary>
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