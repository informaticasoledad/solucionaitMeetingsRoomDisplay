using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;

namespace MeetingRoom.Infrastructure.Services;

public class ZohoCalendarProvider : ICalendarProvider, IDisposable
{
    private readonly HttpClient _client = new();
    private bool _initialized;

    private string _clientId = string.Empty;
    private string _clientSecret = string.Empty;
    private string _refreshToken = string.Empty;

    public Task InitializeAsync(string credentialsJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(credentialsJson);
        var root = doc.RootElement;
        _clientId = root.GetProperty("clientId").GetString()!;
        _clientSecret = root.GetProperty("clientSecret").GetString()!;
        _refreshToken = root.GetProperty("refreshToken").GetString()!;
        _initialized = true;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(
        string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        EnsureInitialized();
        var token = await AcquireTokenAsync(ct);
        var url = "https://calendar.zoho.com/api/v1/events"
                + $"?calendarId={Uri.EscapeDataString(calendarId)}"
                + $"&startDateTime={from.ToUnixTimeMilliseconds()}"
                + $"&endDateTime={to.ToUnixTimeMilliseconds()}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);

        var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var zoho = await response.Content.ReadFromJsonAsync<ZohoCalendarView>(ct);
        return (zoho?.Events ?? []).Select(e => new MeetingEvent
        {
            Id = e.Uid ?? string.Empty,
            Summary = e.Title ?? string.Empty,
            Organizer = e.Organizer?.Email ?? string.Empty,
            Start = DateTimeOffset.FromUnixTimeMilliseconds(e.StartDateTime),
            End = DateTimeOffset.FromUnixTimeMilliseconds(e.EndDateTime),
            IsAllDay = e.IsAllDay
        }).ToList();
    }

    public async Task<MeetingEvent> CreateQuickEventAsync(
        string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default)
    {
        EnsureInitialized();
        var token = await AcquireTokenAsync(ct);
        var url = "https://calendar.zoho.com/api/v1/events";

        var payload = new
        {
            calendarId,
            eventData = new
            {
                title = summary,
                startDateTime = start.ToUnixTimeMilliseconds(),
                endDateTime = start.AddMinutes(durationMinutes).ToUnixTimeMilliseconds()
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", token);

        var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ZohoEventResponse>(ct);
        var ev = created!.Data.Events.First();
        return new MeetingEvent
        {
            Id = ev.Uid ?? string.Empty,
            Summary = ev.Title ?? string.Empty,
            Organizer = ev.Organizer?.Email ?? string.Empty,
            Start = DateTimeOffset.FromUnixTimeMilliseconds(ev.StartDateTime),
            End = DateTimeOffset.FromUnixTimeMilliseconds(ev.EndDateTime),
            IsAllDay = false
        };
    }

    public void Dispose() => _client.Dispose();

    private async Task<string> AcquireTokenAsync(CancellationToken ct)
    {
        var url = "https://accounts.zoho.com/oauth/v2/token";
        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = _refreshToken,
            ["grant_type"] = "refresh_token"
        });

        var response = await _client.PostAsync(url, body, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<ZohoTokenResponse>(ct);
        return tokenResponse!.AccessToken;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("Zoho Calendar not configured.");
    }
}

internal sealed record ZohoCalendarView(List<ZohoEvent> Events);
internal sealed record ZohoEvent(
    string? Uid, string? Title, ZohoOrganizer? Organizer,
    long StartDateTime, long EndDateTime, bool IsAllDay);
internal sealed record ZohoOrganizer(string? Email);
internal sealed record ZohoEventResponse(ZohoEventData Data);
internal sealed record ZohoEventData(List<ZohoEvent> Events);
internal sealed record ZohoTokenResponse(string AccessToken, string TokenType, int ExpiresIn);
