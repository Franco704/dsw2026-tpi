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

        //declaracion de que necesita cada entidad
        private readonly IPersistence _persistence;
        private readonly DoctorService _service;

        //Constructor para que antes de cada prueba arranque limpio
        public TestDoctors()
        {
            _persistence = Substitute.For<IPersistence>();
            _service = new DoctorService(_persistence);
        }

        //Declaro un request valido para utilizar en las pruebas
        private static DoctorModel.Request RequestValido()
        {
            return new DoctorModel.Request("Rusconi Mateo", "12345", Guid.NewGuid());
        }

        //Camino A request inválido ValidationException 
        [Fact]
        public async Task Create_CuandoInstanciamosConNombreInvalido_EntoncesLanzaValidationException()
        {
            // Arrange
            var request = new DoctorModel.Request("Gr", "M-4021", Guid.NewGuid());

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() => _service.Create(request));
        }

        // ── Camino B matrícula duplicada ConflictException
        [Fact]
        public async Task Create_CuandoInstanciamosConMatriculaDuplicada_EntoncesLanzaConflictException()
        {
            // Arrange
            var request = RequestValido();
            var doctorExistente = new Doctor("Otro", request.LicenseNumber,
                new Specialty("Cardio", "descripcion de cardio"));

            // el fake "encuentra" un doctor con esa matrícula
            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns(doctorExistente);

            // Act + Assert
            await Assert.ThrowsAsync<ConflictException>(() => _service.Create(request));
        }

        // Camino C especialidad inexistente EntityNotFoundException
        [Fact]
        public async Task Create_CuandoInstanciamosConEspecialidadInexistente_EntoncesLanzaEntityNotFoundException()
        {
            // Arrange
            var request = RequestValido();
            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns((Doctor?)null); 
            _persistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                        .Returns((Specialty?)null); 

            // Act + Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.Create(request));
        }

        // ── Camino D crea y devuelve Response 
        [Fact]
        public async Task Create_CuandoInstanciamosConDatosValidos_EntoncesCreaYDevuelveResponse()
        {
            // Arrange
            var request = RequestValido();
            var especialidad = new Specialty("Cardio", "descripcion de cardio", request.SpecialtyId);

            _persistence.First<Doctor>(Arg.Any<Expression<Func<Doctor, bool>>>(), Arg.Any<string[]>())
                        .Returns((Doctor?)null);
            _persistence.GetById<Specialty>(Arg.Any<Guid>(), Arg.Any<string[]>())
                        .Returns(especialidad);

            _persistence.Add(Arg.Any<Doctor>())
                .Returns(callInfo => callInfo.Arg<Doctor>());


            // Act
            var response = await _service.Create(request);

            // Assert
            Assert.Equal(request.Name, response.Name);
            Assert.Equal(request.LicenseNumber, response.LicenseNumber);
            await _persistence.Received(1).Add(Arg.Any<Doctor>());  
        }

    }
}
