using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Dsw2026Tpi.Application.Services;

// Coordina el usuario de Identity con la entidad Patient.
public class PatientAccessService : IPatientAccessService
{
    private readonly IPersistence _persistence;
    private readonly IIdentityAccessService _identityAccessService;
    private readonly ILogger<PatientAccessService> _logger;

    public PatientAccessService(
        IPersistence persistence,
        IIdentityAccessService identityAccessService,
        ILogger<PatientAccessService> logger)
    {
        // Permite consultar y guardar entidades del dominio.
        _persistence = persistence;

        // Encapsula las operaciones realizadas con Identity.
        _identityAccessService = identityAccessService;

        // Permite registrar situaciones relevantes del login.
        _logger = logger;
    }

    public async Task<Patient> AuthenticateOrCreateAsync(
        string email,
        long dni)
    {
        // Normaliza el email utilizado durante el proceso.
        var normalizedEmail = email.Trim().ToLowerInvariant();

        // Convierte el DNI numérico al formato varchar del modelo.
        var dniText = dni.ToString(CultureInfo.InvariantCulture);

        // Busca el usuario técnico mediante Identity.
        var user = await _identityAccessService.FindByEmailAsync(
            normalizedEmail);

        // Busca si el DNI ya pertenece a algún paciente.
        var patientByDni = await _persistence.First<Patient>(
            patient => patient.Dni == dniText);

        // Resuelve el primer acceso cuando el usuario todavía no existe.
        if (user is null)
        {
            // Impide asociar un DNI existente a un nuevo usuario.
            if (patientByDni is not null)
            {
                _logger.LogWarning(
                    "Intento de login con datos de paciente inconsistentes.");

                throw new AuthenticationException();
            }

            // Crea el usuario sin contraseña y le asigna el rol Paciente.
            user = await _identityAccessService
                .CreateWithoutPasswordAsync(
                    normalizedEmail,
                    Roles.Patient);

            // Crea el perfil de dominio asociado al usuario.
            var newPatient = new Patient(
                user.Id,
                dniText);

            // Guarda el paciente en la base de dominio.
            return await _persistence.Add(newPatient);
        }

        // Rechaza usuarios eliminados lógicamente.
        if (user.Deleted)
        {
            _logger.LogWarning(
                "Intento de login con usuario eliminado: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        // Comprueba que el usuario tenga el rol Paciente.
        await _identityAccessService.EnsureRoleAsync(
            user,
            Roles.Patient);

        // Busca el paciente asociado al usuario.
        var patientByUser = await _persistence.First<Patient>(
            patient => patient.UserId == user.Id);

        // Crea el perfil si el usuario existe pero todavía no tiene Patient.
        if (patientByUser is null)
        {
            // Impide reutilizar el DNI de otro paciente.
            if (patientByDni is not null)
            {
                _logger.LogWarning(
                    "Intento de asociar un DNI existente al usuario {Email}.",
                    normalizedEmail);

                throw new AuthenticationException();
            }

            // Crea el perfil faltante para el usuario paciente.
            var newPatient = new Patient(
                user.Id,
                dniText);

            // Guarda y retorna el nuevo perfil.
            return await _persistence.Add(newPatient);
        }

        // Rechaza pacientes eliminados o con un DNI diferente.
        if (patientByUser.Deleted ||
            patientByUser.Dni != dniText)
        {
            _logger.LogWarning(
                "Intento de login con datos de paciente inválidos: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        // Retorna el paciente correctamente autenticado.
        return patientByUser;
    }
}