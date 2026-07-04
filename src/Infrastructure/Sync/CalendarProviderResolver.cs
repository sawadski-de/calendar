using Application.Sync;

namespace Infrastructure.Sync;

/// <summary>
/// Takes <see cref="ICalendarProvider"/> (not the concrete Google/Outlook types) so this class is
/// testable with plain fakes — the DI registration (Api/Worker <c>Program.cs</c>) is what actually
/// disambiguates which concrete instance is "Google" vs. "Outlook" via an explicit factory, since two
/// same-typed constructor parameters can't be resolved positionally by the container on their own.
/// </summary>
public class CalendarProviderResolver(ICalendarProvider googleProvider, ICalendarProvider outlookProvider)
    : ICalendarProviderResolver
{
    public ICalendarProvider Resolve(string provider) => provider switch
    {
        CalendarProviders.Google => googleProvider,
        CalendarProviders.Outlook => outlookProvider,
        _ => throw new InvalidOperationException($"Unknown calendar provider: '{provider}'."),
    };
}
