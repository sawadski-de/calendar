namespace Application.Sync;

/// <summary>Stable provider identifiers stored in <c>CalendarConnection.Provider</c>/<c>Appointment.Provider</c>.</summary>
public static class CalendarProviders
{
    public const string Google = "Google";
    public const string Outlook = "Outlook";

    public static readonly IReadOnlyList<string> All = [Google, Outlook];
}
