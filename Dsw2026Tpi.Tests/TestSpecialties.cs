using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Tests
{
    public class TestSpecialties
    {
        [Theory]
        [InlineData(true, false)]   // filtro por activas: la inactiva no pasa
        [InlineData(null, true)]    // sin filtro (admin): pasan las dos
        public async Task GetAll_WhenFilteringByStatus_ThenLetsTheRightOnesThrough(
            bool? onlyActive,
            bool inactivePasses)
        {
            var persistence = Substitute.For<IPersistence>();
            var service = new SpecialitiesService(persistence);
            var active = new Specialty("Cardiology", "cardiology description");
            var inactive = new Specialty("Dermatology", "dermatology description", isActive: false);
            Expression<Func<Specialty, bool>>? filter = null;

            persistence.Paginate(
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Do<Expression<Func<Specialty, bool>>>(f => filter = f),
                    Arg.Any<Expression<Func<Specialty, string>>>(),
                    Arg.Any<string[]>())
                .Returns(new Pagination<Specialty>(10, 1, 0, []));

            await service.GetAllSpecialtiesAsync(10, 1, isActive: onlyActive);

            var passes = filter!.Compile();
            Assert.True(passes(active));
            Assert.Equal(inactivePasses, passes(inactive));
        }
    }
}
