using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Mappers;

public static class SpecialtyMapper
{
    public static SpecialtyModel.Response ToResponse(
        Specialty specialty)
    {
        ArgumentNullException.ThrowIfNull(
            specialty);

        return new SpecialtyModel.Response(
            Id:
                specialty.Id,

            Name:
                specialty.Name,

            Description:
                specialty.Description,

            IsActive:
                specialty.IsActive);
    }
}
