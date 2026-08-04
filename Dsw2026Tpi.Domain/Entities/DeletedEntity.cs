namespace Dsw2026Tpi.Domain.Entities;

public abstract class DeletedEntity : EntityBase
{
    public bool Deleted { get; private set; }

    protected DeletedEntity()
        : this(null)
    {
    }

    protected DeletedEntity(
        Guid? id)
        : base(id)
    {
        Deleted = false;
    }

    public void Delete()
    {
        if (Deleted)
        {
            return;
        }

        Deleted = true;
        MarkAsUpdated();
    }

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
