using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Tests
{
    public class TestDoctors
    {

        private readonly IPersistence _persistence;
        private readonly DoctorService _service;

        public TestDoctors()
        {
            _persistence = Substitute.For<IPersistence>();
            _service = new DoctorService(_persistence);
        }

        private static DoctorModel.Request ValidRequest()
        {
            return new DoctorModel.Request("Rusconi Mateo", "12345", Guid.NewGuid());
        }

        [Fact]
        public async Task Create_WhenNameIsInvalid_ThenThrowsValidationException()
        {
            var request = new DoctorModel.Request("Gr", "M-4021", Guid.NewGuid());

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDoctorAsync(request));
        }

        [Fact]
        public async Task Create_WhenLicenseNumberIsDuplicated_ThenThrowsConflictException()
        {
            var request = ValidRequest();
            var existingDoctor = new Doctor("Otro", request.LicenseNumber,
                new Specialty("Cardio", "cardiology description"));

            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns(existingDoctor);

            await Assert.ThrowsAsync<ConflictException>(() => _service.CreateDoctorAsync(request));
        }

        [Fact]
        public async Task Create_WhenSpecialtyDoesNotExist_ThenThrowsEntityNotFoundException()
        {
            var request = ValidRequest();
            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns((Doctor?)null);
            _persistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                        .Returns((Specialty?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.CreateDoctorAsync(request));
        }

        [Fact]
        public async Task Create_WhenDataIsValid_ThenCreatesAndReturnsResponse()
        {
            var request = ValidRequest();
            var specialty = new Specialty("Cardio", "cardiology description", request.SpecialtyId);

            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns((Doctor?)null);
            _persistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                        .Returns(specialty);

            _persistence.Add(Arg.Any<Doctor>())
                .Returns(callInfo => callInfo.Arg<Doctor>());


            var response = await _service.CreateDoctorAsync(request);

            Assert.Equal(request.Name, response.Name);
            Assert.Equal(request.LicenseNumber, response.LicenseNumber);
            await _persistence.Received(1).Add(Arg.Any<Doctor>());
            await _persistence.Received(1).SaveChangesAsync();
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetAll_WhenFilteringBySpecialtyOrNot_ThenFilterLetsTheRightDoctorsThrough(bool filterBySpecialty)
        {
            var cardiology = new Specialty("Cardio", "cardiology description", Guid.NewGuid());
            var traumatology = new Specialty("Trauma", "traumatology description", Guid.NewGuid());
            var cardiologyDoctor = new Doctor("Rusconi Mateo", "111", cardiology);
            var traumatologyDoctor = new Doctor("Martinez Franco", "222", traumatology);
            Expression<Func<Doctor, bool>>? filter = null;
 
            _persistence.Paginate(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Do<Expression<Func<Doctor, bool>>>(f => filter = f),
                    Arg.Any<Expression<Func<Doctor, string>>>(),
                    Arg.Any<string[]>())
                .Returns(new Pagination<Doctor>(10, 1, 0, []));
 
            await _service.GetAllDoctorsAsync(10, 1, specialtyId: filterBySpecialty ? cardiology.Id : null);
 
            var passes = filter!.Compile();
            Assert.True(passes(cardiologyDoctor));
            Assert.Equal(!filterBySpecialty, passes(traumatologyDoctor));
        }
        [Fact]
        public async Task GetSlots_WhenRangeExceeds31Days_ThenThrowsValidationException()
        {
            var from = DateTime.Today;

            await Assert.ThrowsAsync<ValidationException>(() =>
                _service.GetDoctorSlotsAsync(Guid.NewGuid(), from, from.AddDays(40)));
        }

        [Fact]
        public async Task GetSlots_WhenDoctorDoesNotExist_ThenThrowsEntityNotFoundException()
        {
            _persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns((Doctor?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _service.GetDoctorSlotsAsync(Guid.NewGuid(), DateTime.Today, DateTime.Today.AddDays(5)));
        }

        [Fact]
        public async Task GetSlots_WhenTodaySlotsAlreadyPassed_ThenDoesNotReturnThem()
        {
            var doctor = new Doctor("Rusconi Mateo", "111",
                new Specialty("Cardio", "cardiology description", Guid.NewGuid()));
            var pastSlot = new Availability(doctor.Id, DateTime.Today,
                TimeSpan.Zero, TimeSpan.FromMinutes(30));
            var futureSlot = new Availability(doctor.Id, DateTime.Today.AddDays(1),
                TimeSpan.FromHours(9), TimeSpan.FromHours(9.5));

            _persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(doctor);
            _persistence.GetFiltered(Arg.Any<Expression<Func<Availability, bool>>>(), Arg.Any<string[]>())
                .Returns([pastSlot, futureSlot]);

            var slots = await _service.GetDoctorSlotsAsync(
                doctor.Id, DateTime.Today, DateTime.Today.AddDays(5));

            var slot = Assert.Single(slots);
            Assert.Equal(futureSlot.Id, slot.Id);
        }
    
        [Fact]
        public async Task Delete_WhenDoctorHasFutureAppointments_ThenThrowsConflictException()
        {
            var doctor = new Doctor("Rusconi Mateo", "111",
                new Specialty("Cardiology", "cardiology description", Guid.NewGuid()));

            _persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(doctor);
            _persistence.First<Appointment>(Arg.Any<Expression<Func<Appointment, bool>>>(), Arg.Any<string[]>())
                .Returns(new Appointment(doctor.Id, Guid.NewGuid(), Guid.NewGuid(),
                    DateTime.Now.AddDays(1), "Annual checkup"));

            await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteDoctorAsync(doctor.Id));
            Assert.True(doctor.IsActive);
        }

        [Fact]
        public async Task Delete_WhenDoctorHasNoFutureAppointments_ThenDeactivatesIt()
        {
            var doctor = new Doctor("Rusconi Mateo", "111",
                new Specialty("Cardiology", "cardiology description", Guid.NewGuid()));

            _persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(doctor);

            await _service.DeleteDoctorAsync(doctor.Id);

            Assert.False(doctor.IsActive);
            await _persistence.Received(1).SaveChangesAsync();
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async Task GetSlots_WhenADateIsMissing_ThenThrowsValidationException(bool sendFrom, bool sendTo)
        {
            DateTime? from = sendFrom ? DateTime.Today : null;
            DateTime? to = sendTo ? DateTime.Today.AddDays(5) : null;

            await Assert.ThrowsAsync<ValidationException>(() =>
                _service.GetDoctorSlotsAsync(Guid.NewGuid(), from, to));
        }

        [Fact]
        public async Task Create_WhenLicenseNumberExceeds50Characters_ThenThrowsValidationException()
        {
            var request = new DoctorModel.Request("Rusconi Mateo", new string('1', 51), Guid.NewGuid());

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDoctorAsync(request));
        }
    }


    
}
