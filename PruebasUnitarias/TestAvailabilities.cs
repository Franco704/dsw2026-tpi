using Dsw2026Tpi.Domain.Entities;

namespace PruebasUnitarias
{
    public class TestAvailabilities
    {
        [Theory]
        [InlineData(9.0, 9.5, true)]    // mismo bloque
        [InlineData(9.25, 9.75, true)]  // se pisan en parte
        [InlineData(9.5, 10.0, false)]  // empieza justo cuando termina el otro
        [InlineData(8.5, 9.0, false)]   // termina justo cuando empieza el otro
        public void OverlapsWith_CuandoSeComparaConUnTurnoReservado_EntoncesDetectaLaSuperposicion(
            double inicio,
            double fin,
            bool seSuperpone)
        {
            var medico = Guid.NewGuid();
            var fecha = DateTime.Today.AddDays(1);
            var reservado = new Availability(medico, fecha,
                TimeSpan.FromHours(9), TimeSpan.FromHours(9.5));
            var nuevo = new Availability(medico, fecha,
                TimeSpan.FromHours(inicio), TimeSpan.FromHours(fin));

            Assert.Equal(seSuperpone, nuevo.OverlapsWith(reservado));
        }
    }
}
