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

    }
}
