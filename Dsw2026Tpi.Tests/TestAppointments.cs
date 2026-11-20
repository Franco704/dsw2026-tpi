using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Tests
{
    public class TestAppointments
    {
        private static Appointment NewAppointment() =>
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTime.Now.AddDays(1), "Annual checkup");

        [Fact]
        public void MarkAsAttended_WhenAppointmentHasNotStarted_ThenThrowsConflictException()
        {
            var appointment = NewAppointment();

            Assert.Throws<ConflictException>(() => appointment.MarkAsAttended(DateTime.Now));
            Assert.Equal(AppointmentStatus.BOOKED, appointment.Status);
        }

        [Fact]
        public void MarkAsAttended_WhenAppointmentHasPassed_ThenIsAttendedAndCannotBeCancelled()
        {
            var appointment = NewAppointment();

            appointment.MarkAsAttended(appointment.ScheduledAt.AddHours(1));

            Assert.Equal(AppointmentStatus.ATTENDED, appointment.Status);
            Assert.Throws<ConflictException>(() => appointment.Cancel());
        }

        [Fact]
        public void MarkAsNoShow_WhenAppointmentWasCancelled_ThenThrowsConflictException()
        {
            var appointment = NewAppointment();
            appointment.Cancel();

            Assert.Throws<ConflictException>(() => appointment.MarkAsNoShow(appointment.ScheduledAt.AddHours(1)));
        }

        [Fact]
        public void Cancel_WhenAppointmentWasAlreadyCancelled_ThenThrowsConflictException()
        {
            var appointment = new Appointment(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                DateTime.Now.AddDays(1), "Annual checkup");

            appointment.Cancel();

            var error = Assert.Throws<ConflictException>(() => appointment.Cancel());
            Assert.Equal(409, error.StatusCode);
        }
    }
}