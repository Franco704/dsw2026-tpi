using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Patient : EntityBase
    {
        public string UserId { get; private set; }

        public string Dni { get; private set; }

        public string? FullName { get; private set; }

        
        public bool Deleted { get; private set; }

   

        private Patient()
        {
        }



        

        public Patient(
            string userId,
            string dni,
            Guid? id = null) : base(id)
        {
            // Relaciona el paciente con su usuario de Identity.
            UserId = userId;

            // Guarda el DNI validado previamente.
            Dni = dni;

            // El nombre todavía no está disponible en el primer acceso.
            FullName = null;

            // Todo paciente nuevo comienza activo.
            Deleted = false;

            // Inicializa las fechas heredadas.
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Permite completar posteriormente el perfil.
        public void SetFullName(string fullName)
        {
            // Asigna el nombre recibido.
            FullName = fullName;

            // Registra cuándo se modificó el paciente.
            UpdatedAt = DateTime.UtcNow;
        }

        // Realiza la eliminación lógica.
        public void Delete()
        {
            // Marca al paciente como eliminado.
            Deleted = true;

            // Registra la modificación.
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
