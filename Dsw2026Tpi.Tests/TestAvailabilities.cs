using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Tests
{
    public class TestAvailabilities
    {
        [Theory]
        [InlineData(9.0, 9.5, true)]
        [InlineData(9.25, 9.75, true)]
        [InlineData(9.5, 10.0, false)]
        [InlineData(8.5, 9.0, false)]
        public void OverlapsWith_WhenComparedWithBookedSlot_ThenDetectsOverlap(
            double start,
            double end,
            bool overlaps)
        {
            var doctorId = Guid.NewGuid();
            var date = DateTime.Today.AddDays(1);
            var booked = new Availability(doctorId, date,
                TimeSpan.FromHours(9), TimeSpan.FromHours(9.5));
            var candidate = new Availability(doctorId, date,
                TimeSpan.FromHours(start), TimeSpan.FromHours(end));

            Assert.Equal(overlaps, candidate.OverlapsWith(booked));
        }
    }
}
