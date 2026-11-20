namespace Dsw2026Tpi.Tests
{
    internal sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTime _now;

        public FixedTimeProvider(DateTime now)
        {
            _now = now;
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() =>
            new(DateTime.SpecifyKind(_now, DateTimeKind.Unspecified), TimeSpan.Zero);
    }
}
