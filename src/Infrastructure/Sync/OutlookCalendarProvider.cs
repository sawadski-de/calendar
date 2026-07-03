using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Application.Sync;
using Domain;

namespace Infrastructure.Sync;

/// <summary>
/// Reads an Outlook/Microsoft 365 account's calendar via Microsoft Graph's <c>/me/calendarView</c>
/// REST endpoint. Mirrors <see cref="GoogleCalendarProvider"/>'s structure exactly (Story 2.1) — this
/// class never issues a POST/PATCH/DELETE against a Graph mutation endpoint (AD-2/FR-7).
/// </summary>
public class OutlookCalendarProvider(
    HttpClient httpClient,
    MicrosoftOAuthClient oAuthClient,
    ITokenEncryption tokenEncryption,
    ICalendarConnectionRepository calendarConnectionRepository,
    TimeProvider timeProvider) : ICalendarProvider
{
    private const string CalendarViewEndpoint = "https://graph.microsoft.com/v1.0/me/calendarView";

    public async Task<IReadOnlyList<ExternalCalendarEvent>> FetchAllEventsAsync(
        CalendarConnection connection,
        SyncWindow window,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetValidAccessTokenAsync(connection, cancellationToken);

        var events = new List<ExternalCalendarEvent>();
        string? nextLink = BuildCalendarViewUrl(window);
        while (nextLink is not null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextLink);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            // Forces Graph to return start/end already converted to UTC — without this header Graph
            // returns times in the mailbox's configured timezone, which would corrupt every synced
            // appointment's stored UTC time (AD: "Speicherung ausschließlich in UTC").
            request.Headers.Add("Prefer", "outlook.timezone=\"UTC\"");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CalendarProviderException("provider_error", $"Microsoft Graph returned {(int)response.StatusCode}.");
            }

            var page = await response.Content.ReadFromJsonAsync<GraphEventsPage>(cancellationToken: cancellationToken);
            if (page is null)
            {
                throw new CalendarProviderException("provider_error", "Microsoft Graph returned an empty body.");
            }

            foreach (var item in page.Value ?? [])
            {
                var mapped = MapEvent(item);
                if (mapped is not null)
                {
                    events.Add(mapped);
                }
            }

            // @odata.nextLink is a full absolute URL already carrying the next page's parameters —
            // unlike Google's pageToken, it must be called directly, not re-wrapped into a new query.
            nextLink = page.NextLink;
        }

        return events;
    }

    /// <summary>
    /// <c>/calendarView</c> (not <c>/events</c>) is what makes Graph expand a recurring series
    /// server-side into one entry per occurrence (AD-15) — Outlook's equivalent of Google's
    /// <c>singleEvents=true</c>. Using <c>/events</c> instead would return series masters and
    /// silently break recurring-event import.
    /// </summary>
    private static string BuildCalendarViewUrl(SyncWindow window)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["startDateTime"] = window.RangeStartUtc.ToString("O");
        query["endDateTime"] = window.RangeEndUtc.ToString("O");
        query["$top"] = "50";
        return $"{CalendarViewEndpoint}?{query}";
    }

    private Task<string> GetValidAccessTokenAsync(CalendarConnection connection, CancellationToken cancellationToken) =>
        CalendarConnectionAccessTokenHelper.GetValidAccessTokenAsync(
            connection, oAuthClient.RefreshAsync, tokenEncryption, calendarConnectionRepository, timeProvider, cancellationToken);

    private static ExternalCalendarEvent? MapEvent(GraphEventItem item)
    {
        // A cancelled meeting instance still appears in the calendarView response with isCancelled
        // true, rather than being absent — treat it as absent-from-snapshot so CalendarSyncService's
        // normal missing-key deletion path handles it, no separate cancellation branch needed.
        if (item.IsCancelled || item.Id is null)
        {
            return null;
        }

        var startUtc = ParseGraphDateTime(item.Start);
        var endUtc = ParseGraphDateTime(item.End);
        if (startUtc is null || endUtc is null)
        {
            return null;
        }

        var attendees = (item.Attendees ?? [])
            .Where(a => a.EmailAddress?.Address is not null)
            .Select(a => new ExternalAttendee(a.EmailAddress!.Address!, a.EmailAddress.Name))
            .ToList();

        return new ExternalCalendarEvent(
            item.Id,
            item.Subject ?? string.Empty,
            startUtc.Value,
            endUtc.Value,
            item.IsAllDay,
            item.Location?.DisplayName,
            attendees);
    }

    /// <summary>
    /// The <c>Prefer: outlook.timezone="UTC"</c> header makes Graph return the time already in UTC,
    /// but the JSON string itself still carries no explicit offset (e.g. <c>"2026-07-04T09:00:00"</c>)
    /// — <see cref="DateTimeOffset.Parse(string)"/> would assume the server's local timezone for an
    /// offset-less string, silently corrupting every stored time. Must parse with
    /// <see cref="DateTimeStyles.AssumeUniversal"/> explicitly.
    /// </summary>
    private static DateTimeOffset? ParseGraphDateTime(GraphDateTimeTimeZone? value)
    {
        if (value?.DateTime is null)
        {
            return null;
        }

        return DateTime.Parse(value.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private class GraphEventsPage
    {
        [JsonPropertyName("value")]
        public List<GraphEventItem>? Value { get; set; }

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    private class GraphEventItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("isCancelled")]
        public bool IsCancelled { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("isAllDay")]
        public bool IsAllDay { get; set; }

        [JsonPropertyName("start")]
        public GraphDateTimeTimeZone? Start { get; set; }

        [JsonPropertyName("end")]
        public GraphDateTimeTimeZone? End { get; set; }

        [JsonPropertyName("location")]
        public GraphLocation? Location { get; set; }

        [JsonPropertyName("attendees")]
        public List<GraphAttendee>? Attendees { get; set; }
    }

    private class GraphDateTimeTimeZone
    {
        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        [JsonPropertyName("timeZone")]
        public string? TimeZone { get; set; }
    }

    private class GraphLocation
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }

    private class GraphAttendee
    {
        [JsonPropertyName("emailAddress")]
        public GraphEmailAddress? EmailAddress { get; set; }
    }

    private class GraphEmailAddress
    {
        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
