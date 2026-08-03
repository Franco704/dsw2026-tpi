using Dsw2026Tpi.Domain.Rules;

namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Representa una especialidad médica.
/// </summary>
public class Specialty : DeletedEntity
{
    /// <summary>
    /// Nombre de la especialidad.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Descripción de la especialidad.
    /// </summary>
    public string Description { get; private set; }

    #region Constructor for EF

#pragma warning disable CS8618

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    private Specialty()
    {
    }

#pragma warning restore CS8618

    #endregion

    /// <summary>
    /// Crea una especialidad con información válida y normalizada.
    /// </summary>
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

    /// <summary>
    /// Desactiva la especialidad mediante eliminación lógica.
    /// </summary>
    public void Deactivate()
    {
        Delete();
    }

    /// <summary>
    /// Actualiza la información principal de la especialidad.
    /// </summary>
    public void UpdateInfo(
        string name,
        string description)
    {
        SetInformation(
            name,
            description);

        MarkAsUpdated();
    }

    /// <summary>
    /// Valida, normaliza y asigna la información
    /// principal de la especialidad.
    /// </summary>
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

        /*
         * La asignación se realiza después de validar ambos valores
         * para evitar actualizar parcialmente la entidad.
         */
        Name = normalizedName;
        Description = normalizedDescription;
    }
}