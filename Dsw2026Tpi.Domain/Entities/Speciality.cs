namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Representa una especialidad médica.
/// </summary>
public class Speciality : DeletedEntity
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
    private Speciality()
    {
    }

#pragma warning restore CS8618

    #endregion

    /// <summary>
    /// Crea una nueva especialidad.
    /// </summary>
    public Speciality(
        string name,
        string description,
        Guid? id = null)
        : base(id)
    {
        Name = name.Trim();
        Description = description.Trim();
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
        Name = name.Trim();
        Description = description.Trim();

        MarkAsUpdated();
    }
}