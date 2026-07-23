using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Models;

public static class AppointmentModel
{
    public record Request(
        Guid DoctorId,
        Guid AvailabilityId,
        PatientRequest? Patient,
        string? Reason);

    public record PatientRequest(
        long Dni);

    public record Response(
        Guid Id,
        Guid DoctorId,
        Guid AvailabilityId,
        Guid PatientId,
        DateTime ScheduledAt,
        string Reason,
        string Status);
}
