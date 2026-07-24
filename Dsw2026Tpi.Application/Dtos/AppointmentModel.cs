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

    public record SearchRequest(
        Guid? SpecialtyId,
        Guid? DoctorId,
        long? Dni,
        DateTime? Date, 
        int PageSize = 10,
        int PageIndex = 1); //Cual seria el tamaño de pagina Ideal? 

    public record SearchResponse(
        Guid AppointmentId,
        Guid? SpecialtyId,
        string Specialty,
        Guid DoctorId,
        string Doctor,
        DateTime AvailableTime,
        string Status);
}