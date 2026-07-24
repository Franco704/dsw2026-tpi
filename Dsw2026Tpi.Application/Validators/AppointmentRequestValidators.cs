using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.CrossCutting.Exceptions;
using System.Globalization;

namespace Dsw2026Tpi.Application.Validators;


    public static class AppointmentRequestValidator
    {
        public static void ValidateCreate(
            AppointmentModel.Request? request)
        {
            var validation = new ValidationException();

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
        else if (request.Patient.Dni < 1_000_000L ||
                 request.Patient.Dni > 99_999_999L)
        {
            validation.WithDetail(
                "patient.dni",
                "must_have_between_7_and_8_digits");
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
    public static void ValidateSearch(
    AppointmentModel.SearchRequest request)
    {
        var validation = new ValidationException();

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

            if (dniLength is < 7 or > 10)
            {
                validation.WithDetail(
                    "dni",
                    "must_have_between_7_and_10_digits");
            }
        }

        if (request.PageSize is < 1 or > 100)
        {
            validation.WithDetail(
                "pageSize",
                "must_be_between_1_and_100");
        }

        if (request.PageIndex < 1)
        {
            validation.WithDetail(
                "pageIndex",
                "must_be_greater_than_zero");
        }

        if (validation.Error.Details.Count > 0)
        {
            throw validation;
        }
    }
}

