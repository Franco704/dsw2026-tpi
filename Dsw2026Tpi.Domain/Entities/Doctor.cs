namespace Dsw2026Tpi.Domain.Entities;

/// <summary>
/// Representa a un médico registrado en el sistema.
/// </summary>
public class Doctor : DeletedEntity
{
    /// <summary>
    /// Nombre completo del médico.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Número de matrícula profesional.
    /// </summary>
    public string LicenseNumber { get; private set; }

    /// <summary>
    /// Indica si el médico se encuentra activo.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Identificador de la especialidad del médico.
    /// </summary>
    public Guid SpecialityId { get; private set; }

    /// <summary>
    /// Especialidad asociada al médico.
    /// </summary>
    public Specialty Speciality { get; private set; } = null!;

    #region Constructor for EF

#pragma warning disable CS8618

    /// <summary>
    /// Constructor requerido por Entity Framework Core.
    /// </summary>
    private Doctor()
    {
    }

#pragma warning restore CS8618

    #endregion

    /// <summary>
    /// Crea un nuevo médico asociado a una especialidad.
    /// </summary>
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

    /// <summary>
    /// Actualiza los datos principales del médico.
    /// </summary>
    public void Update(
        string name,
        string licenseNumber,
        Specialty speciality)
    {
        SetInformation(name, licenseNumber, speciality);
        MarkAsUpdated();
    }

    /// <summary>
    /// Desactiva al médico mediante eliminación lógica.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Delete();
    }

    /// <summary>
    /// Valida y asigna la información principal del médico.
    /// </summary>
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

/*
 * CONSIDERACIONES:
 *
 * - CreatedAt, UpdatedAt y Deleted se administran desde EntityBase.
 * - SpecialityId ahora es obligatorio, igual que en Data.
 * - Speciality y SpecialityId se asignan juntos para evitar inconsistencias.
 * - IsActive y Deleted representan estados similares y podrían resultar redundantes.
 * - La unicidad de LicenseNumber se controla en Data y Application.
 */