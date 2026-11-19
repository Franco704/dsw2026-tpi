using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;

namespace PruebasUnitarias
{
    public class TestAppointments
    {
        private static Appointment NuevoTurno() =>
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTime.Now.AddDays(1), "Control anual");

        [Fact]
        public void MarkAsAttended_CuandoElTurnoTodaviaNoOcurrio_EntoncesLanzaConflictException()
        {
            var turno = NuevoTurno();

            Assert.Throws<ConflictException>(() => turno.MarkAsAttended(DateTime.Now));
            Assert.Equal(AppointmentStatus.BOOKED, turno.Status);
        }

        [Fact]
        public void MarkAsAttended_CuandoElTurnoYaPaso_EntoncesQuedaAtendidoYNoSePuedeCancelar()
        {
            var turno = NuevoTurno();

            turno.MarkAsAttended(turno.ScheduledAt.AddHours(1));

            Assert.Equal(AppointmentStatus.ATTENDED, turno.Status);
            Assert.Throws<ConflictException>(() => turno.Cancel());
        }

        [Fact]
        public void MarkAsNoShow_CuandoElTurnoFueCancelado_EntoncesLanzaConflictException()
        {
            var turno = NuevoTurno();
            turno.Cancel();

            Assert.Throws<ConflictException>(() => turno.MarkAsNoShow(turno.ScheduledAt.AddHours(1)));
        }

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