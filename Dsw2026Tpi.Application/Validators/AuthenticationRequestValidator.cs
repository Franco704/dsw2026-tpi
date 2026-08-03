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


