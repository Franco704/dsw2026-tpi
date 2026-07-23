// Permite lanzar errores con el formato común de la API.
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;

// Permite reutilizar IsEmailValid().
using Dsw2026Tpi.CrossCutting.Helpers;

namespace Dsw2026Tpi.Application.Validators;

// Centraliza las validaciones comunes de autenticación.
public static class AuthenticationRequestValidator
{
    // Valida un email recibido en cualquier flujo de autenticación.
    public static void ValidateEmail(string? email)
    {
        // Comprueba que sea obligatorio y tenga formato válido.
        if (!email.IsEmailValid())
        {
            // Devuelve el campo y el motivo del error.
            throw new ValidationException()
                .WithDetail(
                    "email",
                    "required_or_invalid");
        }
    }

    // Valida la contraseña recibida durante un login.
    public static void ValidateLoginPassword(
        string? password)
    {
        // Comprueba que la contraseña haya sido proporcionada.
        if (string.IsNullOrWhiteSpace(password))
        {
            // Informa que el campo es obligatorio.
            throw new ValidationException()
                .WithDetail(
                    "password",
                    "required");
        }

        // Comprueba el mínimo exigido por la consigna.
        if (password.Length < 8)
        {
            // Informa la longitud mínima requerida.
            throw new ValidationException()
                .WithDetail(
                    "password",
                    "minimum_length_8");
        }
    }

    // Valida el DNI utilizado por el paciente.
    public static void ValidatePatientDni(long dni)
    {
        // Verifica que el DNI tenga 7 u 8 dígitos.
        if (dni < 1_000_000 || dni > 99_999_999)
        {
            // Informa el rango de dígitos permitido.
            throw new ValidationException()
                .WithDetail(
                    "dni",
                    "must_have_7_or_8_digits");
        }
    }
}

public static class DoctorsValidators
{
    public static void ValidateDoctorName(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name) && name.Length < 3 || name.Length > 100)
            throw new ValidationException()
                .WithDetail(
                    "name",
                    "El nombre debe tener entre 3 y 100 caracteres"
                );
    }
    public static void ValidateDoctorRequest(DoctorModel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 100)
            throw new ValidationException()
                .WithDetail(
                    "name",
                    "El nombre debe tener entre 3 y 100 caracteres"
                );
        if (string.IsNullOrWhiteSpace(request.LicenseNumber))
            throw new ValidationException()
                .WithDetail(
                    "licenseNumber",
                    "El número de matrícula no puede estar vacio"
                );
        if (request.SpecialityId == Guid.Empty)
            throw new ValidationException()
                .WithDetail(
                    "specialityId",
                    "La especialidad es obligatoria"
                );
    }
}