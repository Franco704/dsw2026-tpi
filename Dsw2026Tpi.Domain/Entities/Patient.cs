namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Representa el perfil de paciente asociado
/// a un usuario de ASP.NET Core Identity.
/// </summary>
public class Patient : DeletedEntity
{
    /// <summary>
    /// Identificador del usuario asociado en Identity.
    /// </summary>
    public string UserId { get; private set; }

    /// <summary>
    /// Documento Nacional de Identidad del paciente.
    /// </summary>
    public string Dni { get; private set; }

    /// <summary>
    /// Nombre completo del paciente.
    /// Puede estar vacío durante el primer acceso.
    /// </summary>
    public string? FullName { get; private set; }

    #region Constructor for EF

#pragma warning disable CS8618

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    private Patient()
    {
    }

#pragma warning restore CS8618

    #endregion

    /// <summary>
    /// Crea un paciente asociado a un usuario de Identity.
    /// </summary>
    public Patient(
        string userId,
        string dni,
        Guid? id = null)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "El identificador de usuario es obligatorio.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(dni))
        {
            throw new ArgumentException(
                "El DNI es obligatorio.",
                nameof(dni));
        }

        UserId = userId.Trim();
        Dni = dni.Trim();
        FullName = null;
    }

    /// <summary>
    /// Completa o actualiza el nombre del paciente.
    /// </summary>
    public void SetFullName(
        string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "El nombre completo es obligatorio.",
                nameof(fullName));
        }

        FullName = fullName.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Desactiva el paciente mediante eliminación lógica.
    /// </summary>
    public void Deactivate()
    {
        Delete();
    }
}

/*
 * CONSIDERACIONES:
 *
 * - Deleted se hereda de EntityBase y no debe redeclararse.
 * - CreatedAt y UpdatedAt también se inicializan en EntityBase.
 * - UserId es string porque IdentityUser utiliza string como clave.
 * - La unicidad de UserId y Dni se controla en Data y Application.
 * 
 * Importante: La propiedad deleted es propia del paciente, la elimine porque seria pisar la propiedad de la entidad base, tenemos que ver que hacemos con esa property
 */