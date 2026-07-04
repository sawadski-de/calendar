using Application.Sync;
using Domain;

namespace UnitTests.Application.Sync;

public class FakeCalendarProvider : ICalendarProvider
{
    private readonly IReadOnlyList<ExternalCalendarEvent> _events;
    private readonly Exception? _failure;

    private FakeCalendarProvider(IReadOnlyList<ExternalCalendarEvent> events, Exception? failure)
    {
        _events = events;
        _failure = failure;
    }

    public static FakeCalendarProvider Returning(params ExternalCalendarEvent[] events) => new(events, null);

    public static FakeCalendarProvider FailingWith(Exception exception) => new([], exception);

    public Task<IReadOnlyList<ExternalCalendarEvent>> FetchAllEventsAsync(
        CalendarConnection connection,
        SyncWindow window,
        CancellationToken cancellationToken = default)
    {
        if (_failure is not null)
        {
            throw _failure;
        }

        return Task.FromResult(_events);
    }
}
