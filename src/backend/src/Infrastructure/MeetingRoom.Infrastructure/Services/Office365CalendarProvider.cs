using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;

namespace MeetingRoom.Infrastructure.Services;

public class Office365CalendarProvider : ICalendarProvider, IDisposable
{
    private readonly HttpClient _client = new();
    private bool _initialized;

    private string _clientId = string.Empty;
    private string _tenantId = string.Empty;
    private string _clientSecret = string.Empty;

    public Task InitializeAsync(string credentialsJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(credentialsJson);
        var root = doc.RootElement;
        _clientId = root.GetProperty("clientId").GetString()!;
        _tenantId = root.GetProperty("tenantId").GetString()!;
        _clientSecret = root.GetProperty("clientSecret").GetString()!;
        _initialized = true;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(
        string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        EnsureInitialized();
        var token = await AcquireTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/users/{calendarId}/calendar/calendarView"
                + $"?startDateTime={Uri.EscapeDataString(from.UtcDateTime.ToString("o"))}"
                + $"&endDateTime={Uri.EscapeDataString(to.UtcDateTime.ToString("o"))}"
                + "&$top=50&$orderby=start/dateTime";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var graph = await response.Content.ReadFromJsonAsync<GraphCalendarView>(ct);
        return (graph?.Value ?? []).Select(e => new MeetingEvent
        {
            Id = e.Id ?? string.Empty,
            Summary = e.Subject ?? string.Empty,
            Organizer = e.Organizer?.EmailAddress?.Address ?? string.Empty,
            Start = e.Start.DateTime,
            End = e.End.DateTime,
            IsAllDay = e.IsAllDay
        }).ToList();
    }

    public async Task<MeetingEvent> CreateQuickEventAsync(
        string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default)
    {
        EnsureInitialized();
        var token = await AcquireTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/users/{calendarId}/calendar/events";

        var payload = new
        {
            subject = summary,
            start = new { dateTime = start.UtcDateTime.ToString("o"), timeZone = "UTC" },
            end = new { dateTime = start.AddMinutes(durationMinutes).UtcDateTime.ToString("o"), timeZone = "UTC" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<GraphEvent>(ct);
        return new MeetingEvent
        {
            Id = created!.Id ?? string.Empty,
            Summary = created.Subject ?? string.Empty,
            Organizer = created.Organizer?.EmailAddress?.Address ?? string.Empty,
            Start = created.Start.DateTime,
            End = created.End.DateTime,
            IsAllDay = false
        };
    }

    public void Dispose() => _client.Dispose();

    private async Task<string> AcquireTokenAsync(CancellationToken ct)
    {
        var url = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["grant_type"] = "client_credentials"
        });

        var response = await _client.PostAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return tokenResponse!.AccessToken;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("Office 365 Calendar not configured.");
    }
}

internal sealed record GraphCalendarView(List<GraphEvent> Value);
internal sealed record GraphEvent(
    string? Id, string? Subject, GraphOrganizer? Organizer,
    GraphDateTime Start, GraphDateTime End, bool IsAllDay);
internal sealed record GraphOrganizer(GraphEmailAddress? EmailAddress);
internal sealed record GraphEmailAddress(string Address);
internal sealed record GraphDateTime(DateTimeOffset DateTime, string TimeZone);
internal sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
