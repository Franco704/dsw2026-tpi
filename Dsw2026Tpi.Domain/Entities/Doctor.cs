namespace Dsw2026Tpi.Domain.Entities;

public class Doctor : DeletedEntity
{
    public string Name { get; private set; }

    public string LicenseNumber { get; private set; }

    public bool IsActive { get; private set; }

    public Guid SpecialityId { get; private set; }

    public Specialty Speciality { get; private set; } = null!;

    #region Constructor for EF

#pragma warning disable CS8618

    private Doctor()
    {
    }

#pragma warning restore CS8618

    #endregion

    public Doctor(
        string name,
        string licenseNumber,
        Specialty speciality,
        Guid? id = null)
        : base(id)
    {
        SetInformation(name, licenseNumber, speciality);
        IsActive = true;
    }

    public void Update(
        string name,
        string licenseNumber,
        Specialty speciality)
    {
        SetInformation(name, licenseNumber, speciality);
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Delete();
    }

    private void SetInformation(
        string name,
        string licenseNumber,
        Specialty speciality)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre del médico es obligatorio.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new ArgumentException(
                "La matrícula del médico es obligatoria.",
                nameof(licenseNumber));
        }

        ArgumentNullException.ThrowIfNull(speciality);

        Name = name.Trim();
        LicenseNumber = licenseNumber.Trim();
        Speciality = speciality;
        SpecialityId = speciality.Id;
    }
}

