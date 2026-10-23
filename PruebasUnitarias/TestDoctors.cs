using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using System.Linq.Expressions;

namespace PruebasUnitarias
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

        private static DoctorModel.Request RequestValido()
        {
            return new DoctorModel.Request("Rusconi Mateo", "12345", Guid.NewGuid());
        }

        [Fact]
        public async Task Create_CuandoInstanciamosConNombreInvalido_EntoncesLanzaValidationException()
        {
            var request = new DoctorModel.Request("Gr", "M-4021", Guid.NewGuid());

            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateDoctorAsync(request));
        }

        [Fact]
        public async Task Create_CuandoInstanciamosConMatriculaDuplicada_EntoncesLanzaConflictException()
        {
            var request = RequestValido();
            var doctorExistente = new Doctor("Otro", request.LicenseNumber,
                new Specialty("Cardio", "descripcion de cardio"));

            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns(doctorExistente);

            await Assert.ThrowsAsync<ConflictException>(() => _service.CreateDoctorAsync(request));
        }

        [Fact]
        public async Task Create_CuandoInstanciamosConEspecialidadInexistente_EntoncesLanzaEntityNotFoundException()
        {
            var request = RequestValido();
            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns((Doctor?)null);
            _persistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                        .Returns((Specialty?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.CreateDoctorAsync(request));
        }

        [Fact]
        public async Task Create_CuandoInstanciamosConDatosValidos_EntoncesCreaYDevuelveResponse()
        {
            var request = RequestValido();
            var especialidad = new Specialty("Cardio", "descripcion de cardio", request.SpecialtyId);

            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns((Doctor?)null);
            _persistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                        .Returns(especialidad);

            _persistence.Add(Arg.Any<Doctor>())
                .Returns(callInfo => callInfo.Arg<Doctor>());


            var response = await _service.CreateDoctorAsync(request);

            Assert.Equal(request.Name, response.Name);
            Assert.Equal(request.LicenseNumber, response.LicenseNumber);
            await _persistence.Received(1).Add(Arg.Any<Doctor>());
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetAll_CuandoSeFiltraONoPorEspecialidad_EntoncesElFiltroDejaPasarLosMedicosCorrectos(bool filtrar)
        {
            var cardio = new Specialty("Cardio", "descripcion de cardio", Guid.NewGuid());
            var trauma = new Specialty("Trauma", "descripcion de trauma", Guid.NewGuid());
            var medicoCardio = new Doctor("Rusconi Mateo", "111", cardio);
            var medicoTrauma = new Doctor("Martinez Franco", "222", trauma);
            Expression<Func<Doctor, bool>>? filtro = null;
 
            _persistence.Paginate(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Do<Expression<Func<Doctor, bool>>>(f => filtro = f),
                    Arg.Any<Expression<Func<Doctor, string>>>(),
                    Arg.Any<string[]>())
                .Returns(new Pagination<Doctor>(10, 1, 0, []));
 
            await _service.GetAllDoctorsAsync(10, 1, specialtyId: filtrar ? cardio.Id : null);
 
            var pasa = filtro!.Compile();
            Assert.True(pasa(medicoCardio));
            Assert.Equal(!filtrar, pasa(medicoTrauma));
        }
        [Fact]
        public async Task GetSlots_CuandoElRangoSupera31Dias_EntoncesLanzaValidationException()
        {
            var from = DateTime.Today;

            await Assert.ThrowsAsync<ValidationException>(() =>
                _service.GetDoctorSlotsAsync(Guid.NewGuid(), from, from.AddDays(40)));
        }

        [Fact]
        public async Task GetSlots_CuandoElMedicoNoExiste_EntoncesLanzaEntityNotFoundException()
        {
            _persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns((Doctor?)null);

            await Assert.ThrowsAsync<EntityNotFoundException>(() =>
                _service.GetDoctorSlotsAsync(Guid.NewGuid(), DateTime.Today, DateTime.Today.AddDays(5)));
        }

        [Fact]
        public async Task GetSlots_CuandoHayTurnosDeHoyYaPasados_EntoncesNoLosDevuelve()
        {
            var medico = new Doctor("Rusconi Mateo", "111",
                new Specialty("Cardio", "descripcion de cardio", Guid.NewGuid()));
            var turnoPasado = new Availability(medico.Id, DateTime.Today,
                TimeSpan.Zero, TimeSpan.FromMinutes(30));
            var turnoFuturo = new Availability(medico.Id, DateTime.Today.AddDays(1),
                TimeSpan.FromHours(9), TimeSpan.FromHours(9.5));

            _persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(medico);
            _persistence.GetFiltered(Arg.Any<Expression<Func<Availability, bool>>>(), Arg.Any<string[]>())
                .Returns([turnoPasado, turnoFuturo]);

            var slots = await _service.GetDoctorSlotsAsync(
                medico.Id, DateTime.Today, DateTime.Today.AddDays(5));

            var slot = Assert.Single(slots);
            Assert.Equal(turnoFuturo.Id, slot.Id);
        }
    }
    
}
