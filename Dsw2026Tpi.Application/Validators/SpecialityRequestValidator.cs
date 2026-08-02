using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Valida los datos utilizados en las operaciones
/// relacionadas con especialidades.
/// </summary>
public static class SpecialityRequestValidator
{
    /// <summary>
    /// Valida el body utilizado para crear
    /// o actualizar una especialidad.
    /// </summary>
    public static void Validate(
        SpecialtyModel.Request? request)
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

        ValidateDescription(
            request.Description,
            validation);

        ThrowIfInvalid(
            validation);
    }

    /// <summary>
    /// Valida los parámetros utilizados para consultar
    /// la lista paginada de especialidades.
    /// </summary>
    public static void ValidateGetAll(
        int pageSize,
        int pageIndex,
        string? name)
    {
        var validation =
            new ValidationException();

        PaginationRequestValidator.AddValidationDetails(
            pageSize,
            pageIndex,
            validation);

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
                    "El nombre debe tener entre 3 y 100 caracteres.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(name) ||
            name.Trim().Length is < 3 or > 100)
        {
            validation.WithDetail(
                "name",
                "El nombre debe tener entre 3 y 100 caracteres.");
        }
    }

    /// <summary>
    /// Valida la descripción utilizada al crear
    /// o actualizar una especialidad.
    /// </summary>
    private static void ValidateDescription(
        string? description,
        ValidationException validation)
    {
        if (string.IsNullOrWhiteSpace(description) ||
            description.Trim().Length is < 10 or > 100)
        {
            validation.WithDetail(
                "description",
                "La descripción debe tener entre 10 y 100 caracteres.");
        }
    }

    /// <summary>
    /// Lanza la excepción acumulada únicamente
    /// cuando se encontraron errores.
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