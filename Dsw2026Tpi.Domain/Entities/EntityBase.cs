namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Clase base para las entidades del dominio.
/// Centraliza la identidad, auditoría y eliminación lógica.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Identificador único de la entidad.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Fecha y hora de creación de la entidad.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Fecha y hora de la última modificación.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    protected EntityBase()
        : this(null)
    {
    }

    /// <summary>
    /// Inicializa una nueva entidad con identidad y datos de auditoría.
    /// </summary>
    /// <param name="id">
    /// Identificador opcional. Si no se proporciona, se genera uno nuevo.
    /// </param>
    protected EntityBase(Guid? id)
    {
        Id = id ?? Guid.NewGuid();

        CreatedAt = DateTime.Now;
        UpdatedAt = CreatedAt;

    }

    /// <summary>
    /// Actualiza la fecha de última modificación.
    /// Debe invocarse cuando una entidad cambia su estado.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.Now;
    }
}
   



/*
 * DECISIONES:
 *
 * - CreatedAt, UpdatedAt y Deleted ya no pueden modificarse
 *   directamente desde otras capas.
 *
 * - Las entidades derivadas deben llamar a MarkAsUpdated()
 *   cuando cambien su estado.
 *
 * - Delete() representa soft delete. PersistenceEf.Delete()
 *   todavía realiza una eliminación física y deberá revisarse.
 *
 * - Deleted seguirá en EntityBase mientras todas las entidades
 *   del dominio utilicen eliminación lógica.
 */