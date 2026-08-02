using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;
using System.Globalization;

namespace Dsw2026Tpi.Application.Mappers;

/// <summary>
/// Centraliza la conversión de entidades Appointment
/// a los contratos públicos de respuesta.
/// </summary>
public static class AppointmentMapper
{
    /// <summary>
    /// Convierte un turno al response utilizado
    /// por la reserva y las consultas del paciente.
    /// </summary>
    public static AppointmentModel.Response ToResponse(
        Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(
            appointment);

        return new AppointmentModel.Response(
            Id:
                appointment.Id,

            DoctorId:
                appointment.DoctorId,

            AvailabilitySlotId:
                appointment.AvailabilityId,

            PatientId:
                appointment.PatientId,

            ScheduledAt:
                appointment.ScheduledAt,

            Reason:
                appointment.Reason,

            Status:
                appointment.Status.ToString());
    }

    /// <summary>
    /// Convierte un turno al response contractual
    /// de las búsquedas administrativas.
    /// </summary>
    public static AppointmentModel.SearchResponse ToSearchResponse(
        Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(
            appointment);

        return new AppointmentModel.SearchResponse(
            AppointmentsId:
                appointment.Id,

            AppointmentsStatus:
                appointment.Status.ToString(),

            Patient:
                new AppointmentModel.SearchPatientResponse(
                    Dni:
                        long.Parse(
                            appointment.Patient.Dni,
                            CultureInfo.InvariantCulture),

                    FullName:
                        appointment.Patient.FullName ??
                        string.Empty),

            Doctor:
                new AppointmentModel.SearchDoctorResponse(
                    DoctorId:
                        appointment.DoctorId,

                    Name:
                        appointment.Doctor.Name,

                    Specialty:
                        new AppointmentModel.SearchSpecialtyResponse(
                            SpecialtyId:
                                appointment.Doctor.SpecialityId,

                            Name:
                                appointment.Doctor.Speciality?.Name ??
                                string.Empty)));
    }
}