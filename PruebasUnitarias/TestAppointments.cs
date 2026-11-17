using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;

namespace PruebasUnitarias
{
    public class TestAppointments
    {
        [Fact]
        public void Cancel_CuandoElTurnoYaFueCancelado_EntoncesLanzaConflictException()
        {
            var turno = new Appointment(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTime.Now.AddDays(1), "Control anual");

            turno.Cancel();

            var error = Assert.Throws<ConflictException>(() => turno.Cancel());
            Assert.Equal(409, error.StatusCode);
        }
    }
}