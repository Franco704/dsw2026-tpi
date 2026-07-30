using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Data.Identity;

/// <summary>
/// Contexto encargado de persistir usuarios, roles,
/// claims, tokens y demás componentes de Identity.
/// </summary>
public class AuthenticationDbContext
    : IdentityDbContext<ApplicationUser>
{
    public AuthenticationDbContext(
        DbContextOptions<AuthenticationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        // Aplica primero la configuración interna de Identity.
        base.OnModelCreating(builder);

        // Configura la tabla principal de usuarios.
        builder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("Users");

            user.Property(u => u.Deleted)
                .IsRequired()
                .HasColumnName("deleted")
                .HasDefaultValue(false);

            user.Property(u => u.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at")
                .HasColumnType("datetime2");

            user.Property(u => u.UpdatedAt)
                .IsRequired()
                .HasColumnName("updated_at")
                .HasColumnType("datetime2");
        });

        // Configura las tablas auxiliares utilizadas por Identity.
        builder.Entity<IdentityRole>()
            .ToTable("Roles");

        builder.Entity<IdentityUserRole<string>>()
            .ToTable("UsersRoles");

        builder.Entity<IdentityUserClaim<string>>()
            .ToTable("UsersClaims");

        builder.Entity<IdentityUserLogin<string>>()
            .ToTable("UsersLogins");

        builder.Entity<IdentityRoleClaim<string>>()
            .ToTable("RolesClaims");

        builder.Entity<IdentityUserToken<string>>()
            .ToTable("UsersTokens");
    }
}

/*
 * CONSIDERACIONES:
 *
 * - ApplicationUser es el usuario real de Identity y se guarda en Users.
 * - No debe mapearse IdentityUser y ApplicationUser en tablas separadas.
 * - Deleted debe comprobarse durante autenticación y autorización.
 * - CreatedAt y UpdatedAt deben inicializarse al crear o modificar usuarios.
 */