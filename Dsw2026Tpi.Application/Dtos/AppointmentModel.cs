public static class AppointmentModel
{
    public record Request(
        Guid DoctorId,
        Guid AvailabilityId,
        PatientRequest? Patient,
        string? Reason);

    public record PatientRequest(
        long Dni);

    public record StatusRequest(
        string? Status);

    public record Response(
        Guid Id,
        Guid DoctorId,
        Guid AvailabilityId,
        Guid PatientId,
        DateTime ScheduledAt,
        string Reason,
        string Status,
        string DoctorName,
        string SpecialtyName);

    public record SearchRequest(
        Guid? SpecialtyId,
        Guid? DoctorId,
        string? Patient,
        string? Status,
        DateTime? Date,
        DateTime? DateFrom,
        DateTime? DateTo,
        int PageSize = 10,
        int PageIndex = 1);

    public record SearchResponse(
        Guid AppointmentsId,
        string AppointmentsStatus,
        DateTime ScheduledAt,
        SearchPatientResponse Patient,
        SearchDoctorResponse Doctor);

    public record SearchPatientResponse(
        long Dni,
        string FullName);

    public record SearchDoctorResponse(
        Guid DoctorId,
        string Name,
        SearchSpecialtyResponse Specialty);

    public record SearchSpecialtyResponse(
        Guid SpecialtyId,
        string Name);
}