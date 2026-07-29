using Microsoft.AspNetCore.Identity;

namespace Dsw2026Tpi.Data.Identity;

/// <summary>
/// Usuario utilizado por ASP.NET Core Identity.
/// Agrega auditoría y eliminación lógica.
/// </summary>
public class ApplicationUser : IdentityUser
{
    // Indica si el usuario fue eliminado lógicamente.
    public bool Deleted { get; set; }

    // Fecha de creación del usuario.
    public DateTime CreatedAt { get; set; }

    // Fecha de última modificación.
    public DateTime UpdatedAt { get; set; }
}