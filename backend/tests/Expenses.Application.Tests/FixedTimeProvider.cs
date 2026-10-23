namespace Expenses.Application.Tests;

/// <summary>A clock frozen at a given instant in a given time zone.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow, TimeZoneInfo localTimeZone) : TimeProvider
{
    // Bolivia: UTC-4, no daylight saving. Built by hand so the tests do not depend on the OS time zone database.
    public static readonly TimeZoneInfo Bolivia =
        TimeZoneInfo.CreateCustomTimeZone("UTC-04", TimeSpan.FromHours(-4), "UTC-04", "UTC-04");

    public override DateTimeOffset GetUtcNow() => utcNow;

    public override TimeZoneInfo LocalTimeZone => localTimeZone;
}
