using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Rules;
using System.Globalization;

namespace Dsw2026Tpi.Application.Validators;

public static class AppointmentRequestValidator
{
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

        if (request.AvailabilityId == Guid.Empty)
        {
            validation.WithDetail(
                "availabilityId",
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
        else
        {
            var reasonLength =
                request.Reason.Trim().Length;

            if (reasonLength <
                AppointmentRules.MinimumReasonLength)
            {
                validation.WithDetail(
                    "reason",
                    "minimum_length_5");
            }
            else if (reasonLength >
                AppointmentRules.MaximumReasonLength)
            {
                validation.WithDetail(
                    "reason",
                    "maximum_length_300");
            }
        }

        validation.ThrowIfAny();
    }

    public static void ValidateSearch(
        AppointmentModel.SearchRequest? request,
        bool dateRequired = false)
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

        if (request.SpecialtyId == Guid.Empty)
        {
            validation.WithDetail(
                "specialtyId",
                "invalid");
        }

        if (request.DoctorId == Guid.Empty)
        {
            validation.WithDetail(
                "doctorId",
                "invalid");
        }

        // Paciente: si son solo dígitos es un DNI, si no, parte del nombre
        var patient = request.Patient?.Trim();

        if (!string.IsNullOrEmpty(patient))
        {
            if (patient.All(char.IsAsciiDigit))
            {
                if (patient.Length is < 7 or > 10)
                {
                    validation.WithDetail(
                        "patient",
                        "dni_must_have_between_7_and_10_digits");
                }
            }
            else if (patient.Length > 100)
            {
                validation.WithDetail(
                    "patient",
                    "maximum_length_100");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !Enum.GetNames<AppointmentStatus>().Contains(
                request.Status.Trim().ToUpperInvariant()))
        {
            validation.WithDetail(
                "status",
                "must_be_booked_cancelled_attended_or_no_show");
        }

        var hasRange =
            request.DateFrom.HasValue ||
            request.DateTo.HasValue;

        if (request.Date.HasValue && hasRange)
        {
            validation.WithDetail(
                "date",
                "use_date_or_date_range_not_both");
        }

        if (request.DateFrom > request.DateTo)
        {
            validation.WithDetail(
                "dateTo",
                "must_not_be_before_date_from");
        }

        if (dateRequired &&
            !request.Date.HasValue &&
            !hasRange)
        {
            validation.WithDetail(
                "date",
                "required");
        }

        PaginationRequestValidator.AddValidationDetails(
            request.PageSize,
            request.PageIndex,
            validation);

        validation.ThrowIfAny();
    }
    public static void ValidateStatusChange(
        Guid appointmentId,
        AppointmentModel.StatusRequest? request)
    {
        var validation = new ValidationException();

        if (appointmentId == Guid.Empty)
        {
            validation.WithDetail(
                "appointmentId",
                "required");
        }

        // El admin solo puede cerrar un turno: atendido o ausente
        if (request?.Status?.Trim().ToUpperInvariant() is not ("ATTENDED" or "NO_SHOW"))
        {
            validation.WithDetail(
                "status",
                "must_be_attended_or_no_show");
        }

        validation.ThrowIfAny();
    }

    public static void ValidateCancel(
        Guid appointmentId)
    {
        if (appointmentId == Guid.Empty)
        {
            throw new ValidationException()
                .WithDetail(
                    "appointmentId",
                    "required");
        }
    }
}