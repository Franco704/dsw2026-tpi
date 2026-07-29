using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Centraliza las validaciones utilizadas en los
/// flujos de autenticación de administradores y pacientes.
/// </summary>
public static class AuthenticationRequestValidator
{
    /// <summary>
    /// Valida que el email sea obligatorio
    /// y tenga un formato válido.
    /// </summary>
    public static void ValidateEmail(
        string? email)
    {
        if (!email.IsEmailValid())
        {
            throw new ValidationException()
                .WithDetail(
                    "email",
                    "required_or_invalid");
        }
    }

    /// <summary>
    /// Valida la contraseña recibida durante
    /// el inicio de sesión de un administrador.
    /// </summary>
    public static void ValidateLoginPassword(
        string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException()
                .WithDetail(
                    "password",
                    "required");
        }

        if (password.Length < 8)
        {
            throw new ValidationException()
                .WithDetail(
                    "password",
                    "minimum_length_8");
        }
    }

    /// <summary>
    /// Valida que el DNI tenga entre siete
    /// y ocho dígitos.
    /// </summary>
    public static void ValidatePatientDni(
        long dni)
    {
        if (dni < 1_000_000L ||
            dni > 99_999_999L)
        {
            throw new ValidationException()
                .WithDetail(
                    "dni",
                    "must_have_7_or_8_digits");
        }
    }
}

/// <summary>
/// Centraliza las validaciones utilizadas al crear
/// o actualizar médicos.
/// </summary>
public static class DoctorsValidators
{
    /// <summary>
    /// Valida los datos requeridos para crear
    /// o actualizar un médico.
    /// </summary>
    public static void ValidateDoctorRequest(
        DoctorModel.Request request)
    {
        var validation = new ValidationException();

        // El nombre es obligatorio y debe respetar su longitud.
        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Length is < 3 or > 100)
        {
            validation.WithDetail(
                "name",
                "El nombre debe tener entre 3 y 100 caracteres.");
        }

        // La matrícula es obligatoria.
        if (string.IsNullOrWhiteSpace(
                request.LicenseNumber))
        {
            validation.WithDetail(
                "licenseNumber",
                "El número de matrícula no puede estar vacío.");
        }

        // La especialidad debe estar identificada.
        if (request.SpecialityId == Guid.Empty)
        {
            validation.WithDetail(
                "specialityId",
                "La especialidad es obligatoria.");
        }

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }
}

/*
 * DECISIONES TOMADAS:
 *
 * - Se mantuvieron AuthenticationRequestValidator
 *   y DoctorsValidators en el mismo archivo.
 *
 * - No se agregaron nuevos códigos a ErrorCodes.
 *
 * - ValidationException sin parámetros utiliza internamente
 *   el mensaje y código VALIDATION_ERROR.
 *
 * - Los nombres de los campos se mantienen en camelCase
 *   para respetar el formato esperado por la API.
 *
 * - Los métodos de autenticación validan un único campo,
 *   por lo que lanzan la excepción inmediatamente.
 *
 * - ValidateDoctorRequest acumula todos los errores antes
 *   de lanzar ValidationException.
 *
 * - Las validaciones del médico continúan siendo utilizadas
 *   tanto por POST como por PUT.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - ValidateDoctorRequest no acepta request nullable.
 *   Actualmente se presupone que ASP.NET Core realiza
 *   correctamente el model binding.
 *
 * - LicenseNumber solo se valida como obligatorio.
 *   Debe confirmarse si también existe una longitud máxima,
 *   por ejemplo 50 caracteres.
 *
 * - La unicidad de LicenseNumber no puede validarse aquí
 *   porque requiere consultar persistencia.
 *
 * - Los detalles de AuthenticationRequestValidator utilizan
 *   códigos técnicos como required y minimum_length_8,
 *   mientras DoctorsValidators utiliza mensajes descriptivos.
 *   Conviene decidir un formato único para toda la API.
 *
 * - La regla de contraseña mínima de ocho caracteres debe
 *   mantenerse sincronizada con PasswordOptions de Identity.
 *
 * - ValidatePatientDni acepta siete u ocho dígitos.
 *   AppointmentRequestValidator y los demás validadores deben
 *   utilizar exactamente la misma regla.
 *
 * - Las dos clases podrían separarse en archivos distintos
 *   para facilitar navegación, pero no es necesario para
 *   el funcionamiento actual.
 */