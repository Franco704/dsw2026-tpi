using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Tests
{
    public class TestDashboard
    {
        private readonly IPersistence _persistence = Substitute.For<IPersistence>();
        private readonly DashboardService _service;

        public TestDashboard()
        {
            _service = new DashboardService(_persistence);
        }

        [Fact]
        public async Task GetStats_WhenDateIsMissing_ThenThrowsValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.GetStatsAsync(null));
        }

        [Fact]
        public async Task GetStats_WhenDatabaseIsEmpty_ThenReturnsZeros()
        {
            var stats = await _service.GetStatsAsync(DateTime.Today);

            Assert.Equal(0, stats.ActiveDoctors);
            Assert.Equal(0, stats.ActiveSpecialties);
            Assert.Equal(0, stats.ConfirmedAppointmentsToday);
        }

        [Fact]
        public async Task GetStats_WhenCountingCatalog_ThenSkipsInactiveAndDeleted()
        {
            Expression<Func<Doctor, bool>>? doctorFilter = null;
            Expression<Func<Specialty, bool>>? specialtyFilter = null;
            _persistence.Count(Arg.Do<Expression<Func<Doctor, bool>>>(f => doctorFilter = f)).Returns(0);
            _persistence.Count(Arg.Do<Expression<Func<Specialty, bool>>>(f => specialtyFilter = f)).Returns(0);

            await _service.GetStatsAsync(DateTime.Today);

            var specialty = new Specialty("Cardiology", "cardiology description");
            var inactiveSpecialty = new Specialty("Dermatology", "dermatology description", isActive: false);
            var deletedSpecialty = new Specialty("Neurology", "neurology description");
            deletedSpecialty.Deactivate();

            var doctor = new Doctor("Rusconi Mateo", "111", specialty);
            var deactivatedDoctor = new Doctor("Martinez Franco", "222", specialty);
            deactivatedDoctor.Deactivate();

            var countsDoctor = doctorFilter!.Compile();
            Assert.True(countsDoctor(doctor));
            Assert.False(countsDoctor(deactivatedDoctor));

            var countsSpecialty = specialtyFilter!.Compile();
            Assert.True(countsSpecialty(specialty));
            Assert.False(countsSpecialty(inactiveSpecialty));
            Assert.False(countsSpecialty(deletedSpecialty));
        }

        [Fact]
        public async Task GetStats_WhenCountingAppointments_ThenOnlyBookedOfThatDayCount()
        {
            Expression<Func<Appointment, bool>>? appointmentFilter = null;
            _persistence.Count(Arg.Do<Expression<Func<Appointment, bool>>>(f => appointmentFilter = f)).Returns(0);

            var day = DateTime.Today.AddDays(10);
            await _service.GetStatsAsync(day.AddHours(15));

            Appointment At(DateTime scheduledAt) =>
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), scheduledAt, "Annual checkup");

            var startOfDay = At(day);
            var endOfDay = At(day.AddDays(1).AddTicks(-1));
            var previousDay = At(day.AddTicks(-1));
            var nextDay = At(day.AddDays(1));
            var cancelled = At(day.AddHours(10));
            cancelled.Cancel();

            var counts = appointmentFilter!.Compile();
            Assert.True(counts(startOfDay));
            Assert.True(counts(endOfDay));
            Assert.False(counts(previousDay));
            Assert.False(counts(nextDay));
            Assert.False(counts(cancelled));
        }
    }
}
