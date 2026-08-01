using Dsw2026Tpi.Api.Services;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Data;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Api.Configurations;

public static class DependencyInjectionConfigurationExtensions
{
    public static IServiceCollection AddAppDependencies(
      this IServiceCollection services)
    {
        services.AddScoped<IPersistence, PersistenceEf>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<ISpecialitiesService, SpecialitiesService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISignInService, SignInService>();
        services.AddScoped<IAvailabilitiesService, AvailabilityService>();
        services.AddSingleton<IFeriadoProvider, FeriadoProvider>();
        // IdentityAccessService utiliza servicios scoped de Identity.
        services.AddScoped<IIdentityAccessService, IdentityAccessService>();
        services.AddScoped<IPatientAccessService, PatientAccessService>();
        services.AddSingleton<JwtService>();

        return services;

    }
}
