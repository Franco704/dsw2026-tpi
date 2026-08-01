using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Valida los datos recibidos al crear o actualizar un mÃ©dico.
/// Solo controla el contenido de la request; la existencia de la
/// especialidad y los conflictos se comprueban en el servicio.
/// </summary>
public static class DoctorRequestValidator
{
    public static void Validate(
        DoctorModel.Request? request)
    {
        var validation = new ValidationException();

        if (request is null)
        {
            validation.WithDetail(
                "request",
                "required");

            throw validation;
        }

        ValidateName(
            request.Name,
            validation);

        ValidateLicenseNumber(
            request.LicenseNumber,
            validation);

        ValidateSpecialtyId(
            request.SpecialtyId,
            validation);

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }

    private static void ValidateName(
        string? name,
        ValidationException validation)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            validation.WithDetail(
                "name",
                "required");

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
}