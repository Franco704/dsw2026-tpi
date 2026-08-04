using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IPatientAccessService
{
    Task<Patient> AuthenticateOrCreateAsync(
        string email,
        long dni);
    Task<Patient> GetAuthenticatedPatientAsync(
        string email);
}