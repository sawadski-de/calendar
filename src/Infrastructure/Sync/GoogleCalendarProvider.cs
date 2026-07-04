using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Web;
using Application.Sync;
using Domain;

namespace Infrastructure.Sync;

/// <summary>
/// Reads a Google account's calendar via the Calendar API's <c>events.list</c> REST endpoint. This
/// class never issues a POST/PATCH/DELETE against a Google mutation endpoint — <see cref="FetchAllEventsAsync"/>
/// is the only public method, matching <see cref="ICalendarProvider"/> exactly (AD-2/FR-7).
/// </summary>
public class GoogleCalendarProvider(
    HttpClient httpClient,
    GoogleOAuthClient oAuthClient,
    ITokenEncryption tokenEncryption,
    ICalendarConnectionRepository calendarConnectionRepository,
    TimeProvider timeProvider) : ICalendarProvider
{
    private const string EventsEndpoint = "https://www.googleapis.com/calendar/v3/calendars/primary/events";

    public async Task<IReadOnlyList<ExternalCalendarEvent>> FetchAllEventsAsync(
        CalendarConnection connection,
        SyncWindow window,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetValidAccessTokenAsync(connection, cancellationToken);

        var events = new List<ExternalCalendarEvent>();
        string? pageToken = null;
        do
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildEventsUrl(window, pageToken));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CalendarProviderException("provider_error", $"Google Calendar API returned {(int)response.StatusCode}.");
            }

            var page = await response.Content.ReadFromJsonAsync<GoogleEventsPage>(cancellationToken: cancellationToken);
            if (page is null)
            {
                throw new CalendarProviderException("provider_error", "Google Calendar API returned an empty body.");
            }

            foreach (var item in page.Items ?? [])
            {
                var mapped = MapEvent(item);
                if (mapped is not null)
                {
                    events.Add(mapped);
                }
            }

            pageToken = page.NextPageToken;
        }
        while (pageToken is not null);

        return events;
    }

    /// <summary>
    /// <c>singleEvents=true</c> is what makes Google expand a recurring series server-side into one
    /// entry per instance (AD-15) — it must never be turned off, that would return the series master
    /// instead of instances and silently break every recurring imported appointment.
    /// </summary>
    private static string BuildEventsUrl(SyncWindow window, string? pageToken)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["timeMin"] = window.RangeStartUtc.ToString("O");
        query["timeMax"] = window.RangeEndUtc.ToString("O");
        query["singleEvents"] = "true";
        query["maxResults"] = "250";
        if (pageToken is not null)
        {
            query["pageToken"] = pageToken;
        }

        return $"{EventsEndpoint}?{query}";
    }

    private Task<string> GetValidAccessTokenAsync(CalendarConnection connection, CancellationToken cancellationToken) =>
        CalendarConnectionAccessTokenHelper.GetValidAccessTokenAsync(
            connection, oAuthClient.RefreshAsync, tokenEncryption, calendarConnectionRepository, timeProvider, cancellationToken);

    private static ExternalCalendarEvent? MapEvent(GoogleEventItem item)
    {
        // "cancelled" events appear in the snapshot instead of just being absent (Google's API
        // behavior) — treating it as absent-from-snapshot lets CalendarSyncService's normal
        // missing-key deletion path handle it, no separate cancellation branch needed.
        if (item.Status == "cancelled" || item.Id is null)
        {
            return null;
        }

        var isAllDay = item.Start?.DateTime is null && item.Start?.Date is not null;
        var startUtc = ParseGoogleDateTime(item.Start);
        var endUtc = ParseGoogleDateTime(item.End);
        if (startUtc is null || endUtc is null)
        {
            return null;
        }

        var attendees = (item.Attendees ?? [])
            .Where(a => a.Email is not null)
            .Select(a => new ExternalAttendee(a.Email!, a.DisplayName))
            .ToList();

        return new ExternalCalendarEvent(
            item.Id,
            item.Summary ?? string.Empty,
            startUtc.Value,
            endUtc.Value,
            isAllDay,
            item.Location,
            attendees);
    }

    private static DateTimeOffset? ParseGoogleDateTime(GoogleEventDateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.DateTime is not null)
        {
            return DateTimeOffset.Parse(value.DateTime);
        }

        // All-day events carry only a date ("2026-07-04"), no time/offset.
        if (value.Date is not null)
        {
            return new DateTimeOffset(DateOnly.Parse(value.Date).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        return null;
    }

    private class GoogleEventsPage
    {
        [JsonPropertyName("items")]
        public List<GoogleEventItem>? Items { get; set; }

        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }
    }

    private class GoogleEventItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("start")]
        public GoogleEventDateTime? Start { get; set; }

        [JsonPropertyName("end")]
        public GoogleEventDateTime? End { get; set; }

        [JsonPropertyName("attendees")]
        public List<GoogleEventAttendee>? Attendees { get; set; }
    }

    private class GoogleEventDateTime
    {
        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }
    }

    private class GoogleEventAttendee
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }
}
