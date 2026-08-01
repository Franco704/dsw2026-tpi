using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace Dsw2026Tpi.Data
{
    internal class FeriadoProvider : IFeriadoProvider
    {
        private readonly HashSet<DateTime> _feriados;

        public FeriadoProvider()
        {

            var ruta = Path.Combine(AppContext.BaseDirectory, "Sources", "Feriados.json");
            var json = File.ReadAllText(ruta);
            var listaFeriados = JsonSerializer.Deserialize<List<Feriado>>(json) ?? new List<Feriado>();
            _feriados = listaFeriados.Select(f => f.Fecha.Date).ToHashSet();
        }
            public bool EsFeriado(DateTime fecha) => _feriados.Contains(fecha);
    }
}
