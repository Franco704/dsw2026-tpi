using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IPatientAccessService
{
    Task<Patient> AuthenticatePatientOrCreateAsync(
        string email,
        long dni);
    Task<Patient> GetAuthenticatedPatientAsync(
        string email);
}
