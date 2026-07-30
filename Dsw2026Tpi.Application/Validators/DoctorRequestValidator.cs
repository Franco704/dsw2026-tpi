using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;

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