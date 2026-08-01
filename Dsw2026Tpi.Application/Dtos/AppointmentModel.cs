public static class AppointmentModel
{
    public record Request(
        Guid DoctorId,
        Guid AvailabilitySlotId,
        PatientRequest? Patient,
        string? Reason);

    public record PatientRequest(
        long Dni);

    public record Response(
        Guid Id,
        Guid DoctorId,
        Guid AvailabilitySlotId,
        Guid PatientId,
        DateTime ScheduledAt,
        string Reason,
        string Status);

    public record SearchRequest(
        Guid? SpecialtyId,
        Guid? DoctorId,
        long? Dni,
        DateTime? Date,
        int PageSize = 10,
        int PageIndex = 1);

    public record SearchResponse(
        string Specialty,
        string Doctor,
        DateTime AvailableTime);
}