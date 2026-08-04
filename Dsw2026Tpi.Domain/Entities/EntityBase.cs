namespace Dsw2026Tpi.Domain.Entities;

public abstract class EntityBase
{
    public Guid Id { get; init; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    protected EntityBase()
        : this(null)
    {
    }

    protected EntityBase(Guid? id)
    {
        Id = id ?? Guid.NewGuid();

        CreatedAt = DateTime.Now;
        UpdatedAt = CreatedAt;

    }

    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTime.Now;
    }
}
   


