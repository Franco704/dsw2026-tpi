

using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Api.Configurations;
public static class IdentitySeedExtensions
{
    public static async Task SeedInitialAdminAsync(
        this WebApplication app)
    {
        var email = app.Configuration["InitialAdmin:Email"]
            ?? throw new InvalidOperationException(
                "No se configuró InitialAdmin:Email.");

        if (!email.IsEmailValid())
        {
            throw new InvalidOperationException(
                "InitialAdmin:Email tiene un formato inválido.");
        }

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();

        var administratorRoleExists =
            await roleManager.RoleExistsAsync(Roles.Administrator);

        if (!administratorRoleExists)
        {
            throw new InvalidOperationException(
                $"No existe el rol {Roles.Administrator}.");
        }

        var administrator =
            await userManager.FindByEmailAsync(email);
        if (administrator is null)
        {
            var password =
                app.Configuration["InitialAdmin:Password"]
                ?? throw new InvalidOperationException(
                    "No se configuró InitialAdmin:Password.");

            administrator = new ApplicationUser
            {
                UserName = email,

                Email = email,

                EmailConfirmed = true,

                Deleted = false,

                CreatedAt = DateTime.UtcNow,

                UpdatedAt = DateTime.UtcNow
            };

            var creationResult =
                await userManager.CreateAsync(
                    administrator,
                    password);

            if (!creationResult.Succeeded)
            {
                var errors = FormatErrors(creationResult);

                throw new InvalidOperationException(
                    $"No se pudo crear el administrador inicial: {errors}");
            }

            logger.LogInformation(
                "Administrador inicial creado: {Email}",
                email);
        }

        var hasAdministratorRole =
            await userManager.IsInRoleAsync(
                administrator,
                Roles.Administrator);

        if (!hasAdministratorRole)
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    administrator,
                    Roles.Administrator);

            if (!roleResult.Succeeded)
            {
                var errors = FormatErrors(roleResult);

                throw new InvalidOperationException(
                    $"No se pudo asignar el rol administrador: {errors}");
            }

            logger.LogInformation(
                "Rol {Role} asignado a {Email}",
                Roles.Administrator,
                email);
        }
    }

    private static string FormatErrors(
        IdentityResult result)
    {
        return string.Join(
            "; ",
            result.Errors.Select(
                error =>
                    $"{error.Code}: {error.Description}"));
    }
}
