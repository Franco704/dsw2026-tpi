using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using System.Globalization;

namespace Dsw2026Tpi.Application.Validators;

/// <summary>
/// Valida las solicitudes relacionadas con la creación
/// y búsqueda de turnos.
/// </summary>
public static class AppointmentRequestValidator
{
    /// <summary>
    /// Valida los datos necesarios para reservar un turno.
    /// Acumula todos los errores antes de lanzar
    /// una ValidationException.
    /// </summary>
    public static void ValidateCreate(
        AppointmentModel.Request? request)
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

        if (request.DoctorId == Guid.Empty)
        {
            validation.WithDetail(
                "doctorId",
                "required");
        }

        if (request.AvailabilitySlotId == Guid.Empty)
        {
            validation.WithDetail(
                "availabilitySlotId",
                "required");
        }

        if (request.Patient is null)
        {
            validation.WithDetail(
                "patient",
                "required");
        }
        else
        {
            var dniLength = request.Patient.Dni
                .ToString(CultureInfo.InvariantCulture)
                .Length;

            if (request.Patient.Dni <= 0 ||
                dniLength is < 7 or > 10)
            {
                validation.WithDetail(
                    "patient.dni",
                    "must_have_between_7_and_10_digits");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            validation.WithDetail(
                "reason",
                "required");
        }
        else if (request.Reason.Trim().Length < 5)
        {
            validation.WithDetail(
                "reason",
                "minimum_length_5");
        }

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }
    /// <summary>
    /// Valida los filtros y parámetros de paginación
    /// utilizados en la búsqueda de turnos.
    /// </summary>
    public static void ValidateSearch(
        AppointmentModel.SearchRequest? request)
    {
        var validation = new ValidationException();

        // La request completa es obligatoria.
        if (request is null)
        {
            validation.WithDetail(
                "request",
                "required");

            throw validation;
        }

        /*
         * Los filtros son opcionales, pero cuando se informan
         * no pueden contener Guid.Empty.
         */
        if (request.SpecialtyId == Guid.Empty)
        {
            validation.WithDetail(
                nameof(request.SpecialtyId),
                "invalid");
        }

        if (request.DoctorId == Guid.Empty)
        {
            validation.WithDetail(
                nameof(request.DoctorId),
                "invalid");
        }

        // Valida la longitud del DNI cuando fue informado.
        if (request.Dni.HasValue)
        {
            var dniLength = request.Dni.Value
                .ToString(CultureInfo.InvariantCulture)
                .Length;

            if (dniLength is < 7 or > 10)
            {
                validation.WithDetail(
                    nameof(request.Dni),
                    "must_have_between_7_and_10_digits");
            }
        }

        // Limita el tamaño permitido de cada página.
        if (request.PageSize is < 1 or > 100)
        {
            validation.WithDetail(
                nameof(request.PageSize),
                "must_be_between_1_and_100");
        }

        // La numeración de páginas comienza en uno.
        if (request.PageIndex < 1)
        {
            validation.WithDetail(
                nameof(request.PageIndex),
                "must_be_greater_than_zero");
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
 * - Se mantuvo la estructura original del validador.
 *
 * - No se extrajeron reglas a métodos auxiliares ni se
 *   incorporaron librerías externas de validación.
 *
 * - Se utiliza ValidationException sin parámetros porque
 *   permite acumular múltiples errores mediante WithDetail.
 *
 * - ValidationException utiliza internamente el mensaje
 *   y código VALIDATION_ERROR.
 *
 * - No se utiliza ErrorCodes directamente porque no existe
 *   un error global distinto para cada campo inválido.
 *
 * - Se agregó control de request nula en ValidateSearch para
 *   evitar una NullReferenceException ante una entrada inválida.
 *
 * - Se utiliza nameof cuando el nombre del campo coincide
 *   correctamente con la propiedad del DTO.
 *
 * - Se conserva "patient.dni" como ruta compuesta para
 *   identificar el campo anidado dentro de Patient.
 *
 * CONSIDERACIONES PARA REVISAR:
 *
 * - ValidateCreate permite DNI de 7 u 8 dígitos, mientras
 *   ValidateSearch permite entre 7 y 10 dígitos.
 *   Debe confirmarse cuál es la regla definitiva del proyecto.
 *
 * - AuthenticationRequestValidator.ValidatePatientDni también
 *   valida el DNI. Conviene mantener la misma regla en todos
 *   los validadores para evitar comportamientos inconsistentes.
 *
 * - Los textos "required", "invalid" y "minimum_length_5"
 *   son códigos técnicos de detalle, no mensajes amigables.
 *   Debe confirmarse si el frontend espera exactamente esos valores.
 *
 * - Los nombres producidos por nameof utilizan PascalCase,
 *   por ejemplo "DoctorId", mientras los literales anteriores
 *   utilizaban camelCase, como "doctorId".
 *
 * - Si el contrato de errores exige camelCase, conviene conservar
 *   los literales originales o aplicar una transformación común.
 *
 * - ValidateSearch considera PageIndex basado en uno.
 *   Debe verificarse que IPersistence.Paginate utilice el mismo
 *   criterio y no espere un índice basado en cero.
 *
 * - No se valida una fecha de búsqueda futura o pasada porque
 *   actualmente cualquier fecha parece permitida como filtro.
 *
 * - La longitud máxima de Reason no se valida aquí. Debe
 *   confirmarse si el DTO, el dominio o la configuración de EF
 *   establecen el límite máximo de 300 caracteres.
 */