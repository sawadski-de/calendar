using Application.Sync;
using Domain;
using Infrastructure.Sync;
using Xunit;

namespace UnitTests.Infrastructure;

public class CalendarProviderResolverTests
{
    [Fact]
    public void Resolve_returns_the_google_provider_for_the_Google_key()
    {
        var google = new FakeProvider();
        var outlook = new FakeProvider();
        var sut = new CalendarProviderResolver(google, outlook);

        Assert.Same(google, sut.Resolve(CalendarProviders.Google));
    }

    [Fact]
    public void Resolve_returns_the_outlook_provider_for_the_Outlook_key()
    {
        var google = new FakeProvider();
        var outlook = new FakeProvider();
        var sut = new CalendarProviderResolver(google, outlook);

        Assert.Same(outlook, sut.Resolve(CalendarProviders.Outlook));
    }

    [Fact]
    public void Resolve_throws_for_an_unknown_provider_string()
    {
        var sut = new CalendarProviderResolver(new FakeProvider(), new FakeProvider());

        Assert.Throws<InvalidOperationException>(() => sut.Resolve("Yahoo"));
    }

    private class FakeProvider : ICalendarProvider
    {
        public Task<IReadOnlyList<ExternalCalendarEvent>> FetchAllEventsAsync(
            CalendarConnection connection, SyncWindow window, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExternalCalendarEvent>>([]);
    }
}
