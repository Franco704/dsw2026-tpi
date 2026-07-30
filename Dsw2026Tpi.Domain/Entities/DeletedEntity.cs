namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Clase base para las entidades que admiten
/// eliminación lógica.
/// </summary>
public abstract class DeletedEntity : EntityBase
{
    /// <summary>
    /// Indica si la entidad fue eliminada lógicamente.
    /// </summary>
    public bool Deleted { get; private set; }

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    protected DeletedEntity()
        : this(null)
    {
    }

    /// <summary>
    /// Inicializa una entidad que admite eliminación lógica.
    /// </summary>
    /// <param name="id">
    /// Identificador opcional de la entidad.
    /// </param>
    protected DeletedEntity(
        Guid? id)
        : base(id)
    {
        Deleted = false;
    }

    /// <summary>
    /// Marca la entidad como eliminada lógicamente.
    /// </summary>
    public void Delete()
    {
        if (Deleted)
        {
            return;
        }

        Deleted = true;
        MarkAsUpdated();
    }

    /// <summary>
    /// Restaura una entidad eliminada lógicamente.
    /// </summary>
    public void Restore()
    {
        if (!Deleted)
        {
            return;
        }

        Deleted = false;
        MarkAsUpdated();
    }
}

/*
 * DECISIONES TOMADAS:
 *
 * - DeletedEntity hereda identidad y auditoría desde EntityBase.
 *
 * - Solo las entidades que admiten eliminación lógica
 *   deben heredar de DeletedEntity.
 *
 * - Deleted tiene setter privado para evitar modificaciones
 *   directas desde Application, API o Data.
 *
 * - Delete y Restore actualizan UpdatedAt mediante
 *   MarkAsUpdated().
 *
 * - Restore se conserva aunque todavía no exista
 *   un caso de uso que lo utilice.
 */