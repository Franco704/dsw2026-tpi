
/*Esta clase la creamos con el fin de cumplir el requerimiento de que una vez iniciado el programa, se cree el admin automaticamente, esto lo implementamos con un seeder.
  pero no es el mismo seeder que db context ya que cargar un superusuario desde un json es una mala practica,usamos en appsetings el email, el cual puede ser publico
  pero la contraseña debe ser secreta, ademas de ser hasheada.
Crear un scope
obtener UserManager y RoleManager
leer email y contraseña
comprobar que exista el rol Administrador
buscar el usuario por email
crearlo si no existe
asignarle el rol Administrador
*/

// Permite utilizar las constantes de roles del sistema.
// Permite utilizar la clase ApplicationUser.
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
// Permite trabajar con UserManager, RoleManager e IdentityResult.
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Api.Configurations;
// Contiene extensiones utilizadas durante la inicialización de la API.
public static class IdentitySeedExtensions
{
    // Extiende WebApplication para ejecutar el seeding desde Program.cs.
    public static async Task SeedInitialAdminAsync(
        this WebApplication app) //No entendemos.
    {
        // Lee el email inicial desde la configuración.
        var email = app.Configuration["InitialAdmin:Email"]
            ?? throw new InvalidOperationException(
                "No se configuró InitialAdmin:Email.");

        // Verifica que el email configurado tenga un formato válido.
        if (!email.IsEmailValid())
        {
            throw new InvalidOperationException(
                "InitialAdmin:Email tiene un formato inválido.");
        }

        // Crea un scope porque UserManager y RoleManager son servicios scoped.
        using var scope = app.Services.CreateScope(); 
        //El using me sirve para desechar la variable en cuanto termine de usarla,rendimiento de memoria. 
        // Obtiene el administrador de usuarios configurado por Identity.
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Obtiene el administrador de roles configurado por Identity.
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>(); //necesitamos profundizar un poco mas esto.

        // Obtiene un logger para registrar el resultado del seeding.
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();

        // Comprueba que el rol Administrador haya sido sembrado previamente.
        var administratorRoleExists =
            await roleManager.RoleExistsAsync(Roles.Administrator);

        // Detiene el inicio si falta un rol requerido por el sistema.
        if (!administratorRoleExists)
        {
            throw new InvalidOperationException(
                $"No existe el rol {Roles.Administrator}.");
        }

        // Busca al administrador por su email normalizado.
        var administrator =
            await userManager.FindByEmailAsync(email); //Funciones que vienen en el framework de Identity. 
        //esto puede ser null, lo de arriba. La clase lo permite? 
        // Solo crea el usuario si todavía no existe.
        if (administrator is null)
        {
            // Lee la contraseña desde User Secrets o variables de entorno.
            var password =
                app.Configuration["InitialAdmin:Password"] //porque busca en configuration? si lo pusimos en secrets.
                ?? throw new InvalidOperationException(
                    "No se configuró InitialAdmin:Password.");

            // Construye el usuario inicial de Identity.
            administrator = new ApplicationUser
            {
                // Identity utiliza UserName para identificar al usuario.
                UserName = email,

                // Guarda el email utilizado para iniciar sesión.
                Email = email,

                // El email se considera confiable porque lo crea el sistema.
                EmailConfirmed = true,

                // El administrador inicial comienza activo.
                Deleted = false,

                // Registra la fecha de creación en UTC.
                CreatedAt = DateTime.UtcNow,

                // Inicializa la última actualización con la fecha de creación.
                UpdatedAt = DateTime.UtcNow
            };

            // Crea el usuario y genera el hash seguro de la contraseña.
            var creationResult =
                await userManager.CreateAsync(
                    administrator,
                    password);

            // Verifica si Identity pudo crear el usuario.
            if (!creationResult.Succeeded)
            {
                // Convierte los errores de Identity en un mensaje legible.
                var errors = FormatErrors(creationResult);

                // Detiene el inicio porque el administrador no pudo crearse.
                throw new InvalidOperationException(
                    $"No se pudo crear el administrador inicial: {errors}");
            }

            // Registra la creación sin mostrar la contraseña.
            logger.LogInformation(
                "Administrador inicial creado: {Email}",
                email);
        }

        // Comprueba si el usuario ya tiene el rol Administrador.
        var hasAdministratorRole =
            await userManager.IsInRoleAsync(
                administrator,
                Roles.Administrator);

        // Asigna el rol solamente si todavía no está relacionado.
        if (!hasAdministratorRole)
        {
            // Crea la relación entre el usuario y el rol.
            var roleResult =
                await userManager.AddToRoleAsync(
                    administrator,
                    Roles.Administrator);

            // Verifica si Identity pudo asignar el rol.
            if (!roleResult.Succeeded)
            {
                // Convierte los errores de Identity en un mensaje legible.
                var errors = FormatErrors(roleResult);

                // Detiene el inicio si el administrador quedó sin su rol.
                throw new InvalidOperationException(
                    $"No se pudo asignar el rol administrador: {errors}");
            }

            // Registra que el rol fue asignado correctamente.
            logger.LogInformation(
                "Rol {Role} asignado a {Email}",
                Roles.Administrator,
                email);
        }
    }

    // Transforma los errores de Identity en una única cadena.
    private static string FormatErrors(
        IdentityResult result)
    {
        // Une el código y la descripción de todos los errores.
        return string.Join(
            "; ",
            result.Errors.Select(
                error =>
                    $"{error.Code}: {error.Description}"));
    }
}
