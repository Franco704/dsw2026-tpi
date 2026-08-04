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
    /// <summary>
    /// Convierte un médico al response contractual utilizando
    /// la especialidad que ya se encuentra cargada en la entidad.
    /// </summary>
    public static DoctorModel.Response ToResponse(
        Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(
            doctor);

        return ToResponse(
            doctor,
            doctor.Speciality);
    }

    /// <summary>
    /// Convierte un médico al response contractual.
    ///
    /// La especialidad puede ser nula cuando fue eliminada
    /// lógicamente, sin que eso elimine u oculte al médico.
    /// </summary>
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