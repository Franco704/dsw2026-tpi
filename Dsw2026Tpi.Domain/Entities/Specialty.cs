using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Domain.Entities;

public class Specialty : DeletedEntity
{
    public string Name { get; private set; }

    public string Description { get; private set; }

    #region Constructor for EF

#pragma warning disable CS8618

    private Specialty()
    {
    }

#pragma warning restore CS8618

    #endregion

    public Specialty(
        string name,
        string description,
        Guid? id = null)
        : base(id)
    {
        SetInformation(
            name,
            description);
    }

    public void Deactivate()
    {
        Delete();
    }

    public void UpdateInfo(
        string name,
        string description)
    {
        SetInformation(
            name,
            description);

        MarkAsUpdated();
    }

    private void SetInformation(
        string name,
        string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre de la especialidad es obligatorio.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "La descripción de la especialidad es obligatoria.",
                nameof(description));
        }

        var normalizedName = name.Trim();
        var normalizedDescription = description.Trim();

        if (normalizedName.Length is
            < SpecialtyRules.MinimumNameLength
            or > SpecialtyRules.MaximumNameLength)
        {
            throw new ArgumentException(
                $"El nombre debe tener entre " +
                $"{SpecialtyRules.MinimumNameLength} y " +
                $"{SpecialtyRules.MaximumNameLength} caracteres.",
                nameof(name));
        }

        if (normalizedDescription.Length is
            < SpecialtyRules.MinimumDescriptionLength
            or > SpecialtyRules.MaximumDescriptionLength)
        {
            throw new ArgumentException(
                $"La descripción debe tener entre " +
                $"{SpecialtyRules.MinimumDescriptionLength} y " +
                $"{SpecialtyRules.MaximumDescriptionLength} caracteres.",
                nameof(description));
        }

        Name = normalizedName;
        Description = normalizedDescription;
    }
}