namespace Dsw2026Tpi.Api.Configurations;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicySettings General { get; init; } = new();

    public RateLimitPolicySettings AdminLogin { get; init; } = new();

    public RateLimitPolicySettings PatientLogin { get; init; } = new();

    public RateLimitPolicySettings AppointmentBooking { get; init; } = new();

    public void Validate()
    {
        General.Validate(
            nameof(General));

        AdminLogin.Validate(
            nameof(AdminLogin));

        PatientLogin.Validate(
            nameof(PatientLogin));

        AppointmentBooking.Validate(
            nameof(AppointmentBooking));
    }
}

public sealed class RateLimitPolicySettings
{
    public int PermitLimit { get; init; }

    public int WindowInSeconds { get; init; }

    public int QueueLimit { get; init; }

    public void Validate(
        string policyName)
    {
        if (PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{policyName}:PermitLimit debe ser mayor que cero.");
        }

        if (WindowInSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{policyName}:WindowInSeconds debe ser mayor que cero.");
        }

        if (QueueLimit != 0)
        {
            throw new InvalidOperationException(
                $"RateLimiting:{policyName}:QueueLimit debe ser cero.");
        }
    }
}