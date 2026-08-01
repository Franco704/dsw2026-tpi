using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Valida los datos utilizados para crear
/// o actualizar una especialidad.
/// </summary>
public static class SpecialityRequestValidator
{
    /// <summary>
    /// Valida el nombre y la descripción de una especialidad.
    /// Acumula todos los errores encontrados antes de lanzar
    /// una ValidationException.
    /// </summary>
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

        if (string.IsNullOrWhiteSpace(request.Name) ||
            request.Name.Trim().Length is < 3 or > 100)
        {
            validation.WithDetail(
                "name",
                "El nombre debe tener entre 3 y 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.Description) ||
            request.Description.Trim().Length is < 10 or > 100)
        {
            validation.WithDetail(
                "description",
                "La descripción debe tener entre 10 y 100 caracteres.");
        }

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }
}