using Application.Sync;

namespace UnitTests.Application.Sync;

/// <summary>Always returns the same fake provider, regardless of the requested provider string —
/// these tests exercise <see cref="CalendarSyncService"/>'s own logic, not provider selection.</summary>
public class FakeCalendarProviderResolver(ICalendarProvider fakeProvider) : ICalendarProviderResolver
{
    public ICalendarProvider Resolve(string provider) => fakeProvider;
}
