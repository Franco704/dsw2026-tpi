using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using System.Linq.Expressions;

namespace PruebasUnitarias
{
    public class TestSpecialties
    {
        [Theory]
        [InlineData(true, false)]   // filtro por activas: la inactiva no pasa
        [InlineData(null, true)]    // sin filtro (admin): pasan las dos
        public async Task GetAll_CuandoSeFiltraPorEstado_EntoncesDejaPasarLasCorrectas(
            bool? soloActivas,
            bool pasaLaInactiva)
        {
            var persistence = Substitute.For<IPersistence>();
            var service = new SpecialitiesService(persistence);
            var activa = new Specialty("Cardiología", "descripcion de cardiologia");
            var inactiva = new Specialty("Dermatología", "descripcion de dermatologia", isActive: false);
            Expression<Func<Specialty, bool>>? filtro = null;

            persistence.Paginate(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Do<Expression<Func<Specialty, bool>>>(f => filtro = f),
                    Arg.Any<Expression<Func<Specialty, string>>>(),
                    Arg.Any<string[]>())
                .Returns(new Pagination<Specialty>(10, 1, 0, []));

            await service.GetAllSpecialtiesAsync(10, 1, isActive: soloActivas);

            var pasa = filtro!.Compile();
            Assert.True(pasa(activa));
            Assert.Equal(pasaLaInactiva, pasa(inactiva));
        }
    }
}
