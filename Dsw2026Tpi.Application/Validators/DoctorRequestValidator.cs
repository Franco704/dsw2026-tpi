using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Valida los datos utilizados en las operaciones
/// relacionadas con médicos.
/// </summary>
public static class DoctorRequestValidator
{
    /// <summary>
    /// Valida el body utilizado para crear
    /// o actualizar un médico.
    /// </summary>
    public static void Validate(
        DoctorModel.Request? request)
    {
        var validation =
            new ValidationException();

        if (request is null)
        {
            validation.WithDetail(
                "request",
                "required");

            throw validation;
        }

        ValidateName(
            request.Name,
            validation,
            isRequired: true);

        ValidateLicenseNumber(
            request.LicenseNumber,
            validation);

        ValidateSpecialtyId(
            request.SpecialtyId,
            validation);

        ThrowIfInvalid(
            validation);
    }

    /// <summary>
    /// Valida los parámetros utilizados para consultar
    /// la lista paginada de médicos.
    /// </summary>
    public static void ValidateGetAll(
        int pageSize,
        int pageIndex,
        string? name)
    {
        var validation =
            new ValidationException();

        if (pageSize is < 1 or > 100)
        {
            validation.WithDetail(
                "pageSize",
                "must_be_between_1_and_100");
        }

        if (pageIndex < 1)
        {
            validation.WithDetail(
                "pageIndex",
                "must_be_greater_than_zero");
        }

        ValidateName(
            name,
            validation,
            isRequired: false);

        ThrowIfInvalid(
            validation);
    }

    /// <summary>
    /// Valida el nombre según sea obligatorio
    /// o corresponda a un filtro opcional.
    /// </summary>
    private static void ValidateName(
        string? name,
        ValidationException validation,
        bool isRequired)
    {
        if (name is null)
        {
            if (isRequired)
            {
                validation.WithDetail(
                    "name",
                    "required");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            validation.WithDetail(
                "name",
                isRequired
                    ? "required"
                    : "length_must_be_between_3_and_100");

            return;
        }

        if (name.Trim().Length is < 3 or > 100)
        {
            validation.WithDetail(
                "name",
                "length_must_be_between_3_and_100");
        }
    }

    private static void ValidateLicenseNumber(
        string? licenseNumber,
        ValidationException validation)
    {
        if (string.IsNullOrWhiteSpace(
                licenseNumber))
        {
            validation.WithDetail(
                "licenseNumber",
                "required");
        }
    }

    private static void ValidateSpecialtyId(
        Guid specialtyId,
        ValidationException validation)
    {
        if (specialtyId == Guid.Empty)
        {
            validation.WithDetail(
                "specialtyId",
                "required");
        }
    }

    /// <summary>
    /// Lanza la excepción acumulada solamente
    /// cuando se detectaron errores.
    /// </summary>
    private static void ThrowIfInvalid(
        ValidationException validation)
    {
        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }
}