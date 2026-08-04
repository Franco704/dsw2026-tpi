namespace Dsw2026Tpi.Domain.Entities;

public class Patient : DeletedEntity
{
    public string UserId { get; private set; }

    public string Dni { get; private set; }

    public string? FullName { get; private set; }

    #region Constructor for EF

#pragma warning disable CS8618

    private Patient()
    {
    }

#pragma warning restore CS8618

    #endregion

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

    public void Deactivate()
    {
        Delete();
    }
}

