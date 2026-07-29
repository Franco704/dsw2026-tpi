using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Dsw2026Tpi.Application.Services;

/// <summary>
/// Coordina los usuarios administrados por Identity
/// con las entidades Patient del dominio.
/// </summary>
public class PatientAccessService : IPatientAccessService
{
    private readonly IPersistence _persistence;
    private readonly IIdentityAccessService _identityAccessService;
    private readonly ILogger<PatientAccessService> _logger;

    /// <summary>
    /// Inicializa el servicio con las abstracciones necesarias
    /// para consultar Identity, persistir pacientes y registrar eventos.
    /// </summary>
    public PatientAccessService(
        IPersistence persistence,
        IIdentityAccessService identityAccessService,
        ILogger<PatientAccessService> logger)
    {
        // Permite consultar y guardar entidades del dominio.
        _persistence = persistence;

        // Encapsula las operaciones realizadas con Identity.
        _identityAccessService = identityAccessService;

        // Registra situaciones relevantes de autenticación.
        _logger = logger;
    }

    /// <summary>
    /// Autentica un paciente mediante email y DNI.
    /// Si es su primer acceso, crea el usuario y el perfil Patient.
    /// </summary>
    public async Task<Patient> AuthenticateOrCreateAsync(
        string email,
        long dni)
    {
        // Normaliza el email utilizado durante todo el proceso.
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        // Convierte el DNI numérico al formato almacenado en el dominio.
        var dniText = dni.ToString(
            CultureInfo.InvariantCulture);

        // Busca el usuario mediante Identity.
        var user =
            await _identityAccessService.FindByEmailAsync(
                normalizedEmail);

        // Busca si el DNI ya pertenece a otro paciente.
        var patientByDni =
            await _persistence.First<Patient>(
                patient =>
                    patient.Dni == dniText);

        /*
         * Resuelve el primer acceso cuando todavía
         * no existe un usuario de Identity.
         */
        if (user is null)
        {
            // Impide asociar un DNI existente a un usuario nuevo.
            if (patientByDni is not null)
            {
                _logger.LogWarning(
                    "Intento de login con datos de paciente inconsistentes.");

                throw new AuthenticationException();
            }

            // Crea el usuario sin contraseña y le asigna el rol Paciente.
            user =
                await _identityAccessService
                    .CreateWithoutPasswordAsync(
                        normalizedEmail,
                        Roles.Patient);

            // Crea el perfil de dominio asociado al usuario.
            var newPatient = new Patient(
                user.Id,
                dniText);

            return await _persistence.Add(
                newPatient);
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

        // Busca el paciente asociado al usuario de Identity.
        var patientByUser =
            await _persistence.First<Patient>(
                patient =>
                    patient.UserId == user.Id);

        /*
         * Crea el perfil faltante cuando el usuario existe
         * pero todavía no posee una entidad Patient.
         */
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

            var newPatient = new Patient(
                user.Id,
                dniText);

            return await _persistence.Add(
                newPatient);
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

        return patientByUser;
    }

    /// <summary>
    /// Obtiene el paciente asociado al usuario autenticado.
    /// </summary>
    public async Task<Patient> GetAuthenticatedPatientAsync(
        string email)
    {
        // Normaliza el email obtenido desde los claims.
        var normalizedEmail = email
            .Trim()
            .ToLowerInvariant();

        // Busca el usuario autenticado mediante Identity.
        var user =
            await _identityAccessService.FindByEmailAsync(
                normalizedEmail);

        if (user is null ||
            user.Deleted)
        {
            _logger.LogWarning(
                "No se pudo resolver el paciente autenticado.");

            throw new AuthorizationException();
        }

        /*
         * Confirma que el usuario pertenece al rol Patient.
         * Este chequeo todavía utiliza EnsureRoleAsync.
         */
        try
        {
            await _identityAccessService.EnsureRoleAsync(
                user,
                Roles.Patient);
        }
        catch (AuthenticationException)
        {
            /*
             * En este caso el usuario ya está autenticado.
             * Por eso se traduce el error de rol a autorización.
             */
            _logger.LogWarning(
                "El usuario autenticado no posee el rol Patient.");

            throw new AuthorizationException();
        }

        // Busca el perfil Patient asociado al usuario.
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

/*
 * DECISIONES TOMADAS:
 *
 * - Se mantuvo la estructura original de PatientAccessService.
 *
 * - No se extrajeron nuevos servicios ni métodos auxiliares.
 *
 * - AuthenticationException se utiliza durante LoginPatient,
 *   porque el flujo todavía corresponde a autenticación.
 *
 * - AuthorizationException se utiliza al resolver el paciente
 *   desde un usuario que ya se encuentra autenticado.
 *
 * - AUTHENTICATION_FAILED y AUTHORIZATION_FAILED continúan
 *   centralizados dentro de las excepciones correspondientes.
 *
 * - No se agregaron códigos nuevos a ErrorCodes.
 *
 * - El DNI se convierte a string utilizando cultura invariante,
 *   manteniendo consistencia con la propiedad Patient.Dni.
 *
 * - Los logs no incluyen el DNI ni información sensible.
 *
 * - El email se normaliza antes de consultar Identity.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - EnsureRoleAsync lanza AuthenticationException porque fue
 *   diseñado para el flujo de login. En GetAuthenticatedPatientAsync
 *   el usuario ya está autenticado, por lo que se traduce el error
 *   a AuthorizationException.
 *
 * - Una alternativa más clara sería disponer de un método separado
 *   para autorización, pero no se agregó para respetar la estructura.
 *
 * - La creación del usuario de Identity y del Patient no es atómica.
 *   Si el usuario se crea y falla _persistence.Add, podría quedar
 *   un usuario sin perfil Patient.
 *
 * - Cuando el usuario existe pero no tiene Patient, la creación del
 *   perfil también puede fallar después de haber validado Identity.
 *
 * - La búsqueda por DNI y la posterior inserción no están protegidas
 *   completamente frente a concurrencia. La base debe conservar
 *   una restricción única sobre Patient.Dni.
 *
 * - Si Patient.UserId también debe ser único, la base debería
 *   conservar una restricción única sobre esa columna.
 *
 * - AuthenticateOrCreateAsync concentra autenticación, reparación
 *   de datos incompletos y creación de entidades. Se mantiene así
 *   para respetar el código original del equipo.
 */