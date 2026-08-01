using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Feriado
    {
        public DateTime Fecha { get; set; }
        public string Dia { get; set; } = "";
        public string Motivo { get; set; } = "";
        public string Tipo { get; set; } = "";
    }

}
