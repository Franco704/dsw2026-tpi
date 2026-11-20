using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dsw2026Tpi.Tests
{
    public class TestAppointmentBooking
    {
        [Fact]
        public async Task Create_WhenDoctorSpecialtyIsInactive_ThenThrowsConflictException()
        {
            var persistence = Substitute.For<IPersistence>();
            var patientAccess = Substitute.For<IPatientAccessService>();
            var service = new AppointmentService(
                persistence,
                patientAccess,
                Substitute.For<ILogger<AppointmentService>>());

            var inactiveSpecialty = new Specialty("Dermatology", "dermatology description", isActive: false);
            var doctor = new Doctor("Rusconi Mateo", "111", inactiveSpecialty);

            patientAccess.GetAuthenticatedPatientAsync(Arg.Any<string>())
                .Returns(new Patient("user-id", "30111222"));
            persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(doctor);

            var request = new AppointmentModel.Request(
                doctor.Id,
                Guid.NewGuid(),
                new AppointmentModel.PatientRequest(30111222),
                "Annual checkup");

            await Assert.ThrowsAsync<ConflictException>(
                () => service.CreateAppointmentAsync(request, "patient@test.com"));
            await persistence.DidNotReceive().SaveChangesAsync();
        }
    }
}
