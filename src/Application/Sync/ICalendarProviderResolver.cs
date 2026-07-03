namespace Application.Sync;

/// <summary>
/// Picks the right <see cref="ICalendarProvider"/> for a <see cref="Domain.CalendarConnection.Provider"/>
/// value. Application only knows the port (<see cref="ICalendarProvider"/>) and this resolver's
/// contract — the concrete Google/Outlook implementations stay in Infrastructure (AD-1), which is
/// also where this interface's implementation lives, since only Infrastructure may reference those
/// concrete types.
/// </summary>
public interface ICalendarProviderResolver
{
    /// <summary>Throws if <paramref name="provider"/> isn't a recognized value from <see cref="CalendarProviders"/>.</summary>
    ICalendarProvider Resolve(string provider);
}
