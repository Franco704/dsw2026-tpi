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

    /*
     * Los nombres AppointmentsId y AppointmentsStatus
     * respetan literalmente el contrato 1.6.
     */
    public record SearchResponse(
        Guid AppointmentsId,
        string AppointmentsStatus,
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