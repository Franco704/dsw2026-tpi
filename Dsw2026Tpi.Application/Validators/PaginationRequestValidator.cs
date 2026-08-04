using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Application.Validators;

public static class PaginationRequestValidator
{
    private const int MinimumPageSize = 1;
    private const int MaximumPageSize = 100;
    private const int MinimumPageIndex = 1;

    public static void AddValidationDetails(
        int pageSize,
        int pageIndex,
        ValidationException validation)
    {
        ArgumentNullException.ThrowIfNull(
            validation);

        if (pageSize is < MinimumPageSize or > MaximumPageSize)
        {
            validation.WithDetail(
                "pageSize",
                "must_be_between_1_and_100");
        }

        if (pageIndex < MinimumPageIndex)
        {
            validation.WithDetail(
                "pageIndex",
                "must_be_greater_than_zero");
        }
        }
}