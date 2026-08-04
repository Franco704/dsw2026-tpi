using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Dsw2026Tpi.Application.Services;

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
        _persistence = persistence;

        _identityAccessService = identityAccessService;

        _logger = logger;
    }

    public async Task<Patient> AuthenticatePatientOrCreateAsync(
        string email,
        long dni)
    {
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        var dniText = dni.ToString(
            CultureInfo.InvariantCulture);

        var user =
            await _identityAccessService.FindUserByEmailAsync(
                normalizedEmail);

        var patientByDni =
            await _persistence.First<Patient>(
                patient =>
                    patient.Dni == dniText);

        if (user is null)
        {
            if (patientByDni is not null)
            {
                _logger.LogWarning(
                    "Intento de login con datos de paciente inconsistentes.");

                throw new AuthenticationException();
            }

            user =
                await _identityAccessService
                    .CreateUserWithoutPasswordAsync(
                        normalizedEmail,
                        Roles.Patient);

            var newPatient = new Patient(
                user.Id,
                dniText);

            return await _persistence.Add(
                newPatient);
        }

        if (user.Deleted)
        {
            _logger.LogWarning(
                "Intento de login con usuario eliminado: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        await _identityAccessService.EnsureUserHasRoleAsync(
            user,
            Roles.Patient);

        var patientByUser =
            await _persistence.First<Patient>(
                patient =>
                    patient.UserId == user.Id);

        if (patientByUser is null)
        {
            if (patientByDni is not null)
            {
                _logger.LogWarning(
                    "Intento de asociar un DNI existente al usuario {Email}.",
                    normalizedEmail);

                throw new AuthenticationException();
            }

            var newPatient = new Patient(
                user.Id,
                dniText);

            return await _persistence.Add(
                newPatient);
        }

        if (patientByUser.Deleted ||
            patientByUser.Dni != dniText)
        {
            _logger.LogWarning(
                "Intento de login con datos de paciente inválidos: {Email}",
                normalizedEmail);

            throw new AuthenticationException();
        }

        return patientByUser;
    }

    public async Task<Patient> GetAuthenticatedPatientAsync(
        string email)
    {
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _identityAccessService.FindUserByEmailAsync(
                normalizedEmail);

        if (user is null ||
            user.Deleted)
        {
            _logger.LogWarning(
                "No se pudo resolver el paciente autenticado.");

            throw new AuthorizationException();
        }

        try
        {
            await _identityAccessService.EnsureUserHasRoleAsync(
                user,
                Roles.Patient);
        }
        catch (AuthenticationException)
        {
            _logger.LogWarning(
                "El usuario autenticado no posee el rol Patient.");

            throw new AuthorizationException();
        }

        var patient =
            await _persistence.First<Patient>(
                patient =>
                    patient.UserId == user.Id);

        if (patient is null ||
            patient.Deleted)
        {
            _logger.LogWarning(
                "El usuario autenticado no posee un paciente activo.");

            throw new AuthorizationException();
        }

        return patient;
    }
}

