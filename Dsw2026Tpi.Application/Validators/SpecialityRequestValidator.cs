using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Application.Validators;

public static class SpecialityRequestValidator
{
    public static void Validate(
        SpecialtyModel.Request? request)
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
            validation,
            isRequired: true);

        ValidateDescription(
            request.Description,
            validation);

        ThrowIfInvalid(validation);
    }

    public static void ValidateGetAll(
        int pageSize,
        int pageIndex,
        string? name)
    {
        var validation = new ValidationException();

        PaginationRequestValidator.AddValidationDetails(
            pageSize,
            pageIndex,
            validation);

        ValidateName(
            name,
            validation,
            isRequired: false);

        ThrowIfInvalid(validation);
    }

    private static void ValidateName(
        string? name,
        ValidationException validation,
        bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            if (isRequired)
            {
                validation.WithDetail(
                    "name",
                    "required");
            }

            return;
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length is
            < SpecialtyRules.MinimumNameLength
            or > SpecialtyRules.MaximumNameLength)
        {
            validation.WithDetail(
                "name",
                "El nombre debe tener entre 3 y 100 caracteres.");
        }
    }

    private static void ValidateDescription(
        string? description,
        ValidationException validation)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            validation.WithDetail(
                "description",
                "La descripción debe tener entre 10 y 100 caracteres.");

            return;
        }

        var normalizedDescription = description.Trim();

        if (normalizedDescription.Length is
            < SpecialtyRules.MinimumDescriptionLength
            or > SpecialtyRules.MaximumDescriptionLength)
        {
            validation.WithDetail(
                "description",
                "La descripción debe tener entre 10 y 100 caracteres.");
        }
    }

    private static void ThrowIfInvalid(
        ValidationException validation)
    {
        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }
}