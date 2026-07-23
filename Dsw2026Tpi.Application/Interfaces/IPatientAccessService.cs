using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IPatientAccessService
{
    //Crea el paciente durante el acceso 
    Task<Patient> AuthenticateOrCreateAsync(
        string email,
        long dni);
    Task<Patient> GetAuthenticatedPatientAsync(
        string email);
}