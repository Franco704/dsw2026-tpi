using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.Application.Settings;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Tests
{
    public class TestTimeAndToken
    {
        [Fact]
        public async Task GetSlots_WhenClockIsFixed_ThenDiscardsSlotsBeforeThatTime()
        {
            var persistence = Substitute.For<IPersistence>();
            var service = new DoctorService(persistence, new FixedTimeProvider(DateTime.Today.AddHours(10)));
            var doctor = new Doctor("Rusconi Mateo", "111",
                new Specialty("Cardiology", "cardiology description", Guid.NewGuid()));
            var morningSlot = new Availability(doctor.Id, DateTime.Today,
                TimeSpan.FromHours(9), TimeSpan.FromHours(9.5));
            var laterSlot = new Availability(doctor.Id, DateTime.Today,
                TimeSpan.FromHours(11), TimeSpan.FromHours(11.5));

            persistence.GetById<Doctor>(Arg.Any<Guid>(), Arg.Any<string[]>())
                .Returns(doctor);
            persistence.GetFiltered(Arg.Any<Expression<Func<Availability, bool>>>(), Arg.Any<string[]>())
                .Returns([morningSlot, laterSlot]);

            var slots = await service.GetDoctorSlotsAsync(doctor.Id, DateTime.Today, DateTime.Today);

            var slot = Assert.Single(slots);
            Assert.Equal(laterSlot.Id, slot.Id);
        }

        [Fact]
        public void GenerateToken_WhenSettingsAreConfigured_ThenUsesThemAndTheClock()
        {
            var now = new DateTime(2026, 11, 20, 10, 0, 0);
            var settings = Options.Create(new JwtSettings
            {
                Key = new string('k', 64),
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiresInMinutes = 30
            });
            var service = new JwtService(settings, new FixedTimeProvider(now));

            var token = new JwtSecurityTokenHandler().ReadJwtToken(
                service.GenerateToken("admin@test.com", "Administrador"));

            Assert.Equal("test-issuer", token.Issuer);
            Assert.Contains("test-audience", token.Audiences);
            Assert.Equal(now.AddMinutes(30), token.ValidTo);
            Assert.Contains(token.Claims, claim => claim.Value == "Administrador");
        }
    }
}
