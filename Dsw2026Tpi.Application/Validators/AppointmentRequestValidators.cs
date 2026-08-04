using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
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

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
    }

    public static void ValidateSearch(
        AppointmentModel.SearchRequest? request)
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

        if (request.Dni.HasValue)
        {
            var dniLength = request.Dni.Value
                .ToString(CultureInfo.InvariantCulture)
                .Length;

            if (request.Dni.Value <= 0 ||
                dniLength is < 7 or > 10)
            {
                validation.WithDetail(
                    "dni",
                    "must_have_between_7_and_10_digits");
            }
        }

        PaginationRequestValidator.AddValidationDetails(
            request.PageSize,
            request.PageIndex,
            validation);

        if (validation.Error.Details.Any())
        {
            throw validation;
        }
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