using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;

namespace MeetingRoom.Infrastructure.Services;

public class CalDavCalendarProvider : ICalendarProvider, IDisposable
{
    private readonly HttpClient _client = new();
    private bool _initialized;

    private string _serverUrl = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    public Task InitializeAsync(string credentialsJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(credentialsJson);
        var root = doc.RootElement;
        _serverUrl = root.GetProperty("url").GetString()!.TrimEnd('/');
        _username = root.GetProperty("username").GetString()!;
        _password = root.GetProperty("password").GetString()!;

        var authBytes = Encoding.UTF8.GetBytes($"{_username}:{_password}");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        _client.DefaultRequestHeaders.Add("Depth", "1");

        _initialized = true;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(
        string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        EnsureInitialized();

        var calendarUrl = string.IsNullOrEmpty(calendarId) ? _serverUrl : $"{_serverUrl}/{calendarId.TrimStart('/')}";
        var body = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <C:calendar-query xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
              <D:prop>
                <D:getetag/>
                <C:calendar-data/>
              </D:prop>
              <C:filter>
                <C:comp-filter name="VCALENDAR">
                  <C:comp-filter name="VEVENT">
                    <C:time-range start="{from.UtcDateTime:yyyyMMddTHHmmssZ}" end="{to.UtcDateTime:yyyyMMddTHHmmssZ}"/>
                  </C:comp-filter>
                </C:comp-filter>
              </C:filter>
            </C:calendar-query>
            """;

        var request = new HttpRequestMessage(new HttpMethod("REPORT"), calendarUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml")
        };

        var response = await _client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync(ct);
        return ParseICalEvents(xml, from, to);
    }

    public async Task<MeetingEvent> CreateQuickEventAsync(
        string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default)
    {
        EnsureInitialized();

        var calendarUrl = string.IsNullOrEmpty(calendarId) ? _serverUrl : $"{_serverUrl}/{calendarId.TrimStart('/')}";
        var uid = $"{Guid.NewGuid()}@caldav-solucionait";
        var now = DateTimeOffset.UtcNow;
        var end = start.AddMinutes(durationMinutes);

        var icsBody = $"""
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//SolucionAIT//MeetingRoomDisplay//ES
            BEGIN:VEVENT
            UID:{uid}
            DTSTAMP:{now:yyyyMMddTHHmmssZ}
            DTSTART:{start.UtcDateTime:yyyyMMddTHHmmssZ}
            DTEND:{end.UtcDateTime:yyyyMMddTHHmmssZ}
            SUMMARY:{EscapeICal(summary)}
            END:VEVENT
            END:VCALENDAR
            """;

        var eventUrl = $"{calendarUrl}/{uid}.ics";
        var putRequest = new HttpRequestMessage(HttpMethod.Put, eventUrl)
        {
            Content = new StringContent(icsBody, Encoding.UTF8, "text/calendar")
        };
        putRequest.Headers.Add("If-None-Match", "*");

        var putResponse = await _client.SendAsync(putRequest, ct);
        putResponse.EnsureSuccessStatusCode();

        return new MeetingEvent
        {
            Id = uid,
            Summary = summary,
            Organizer = _username,
            Start = start,
            End = end,
            IsAllDay = false
        };
    }

    public void Dispose() => _client.Dispose();

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("CalDAV Calendar not configured.");
    }

    private static string EscapeICal(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\n", "\\n");
    }

    private static IReadOnlyList<MeetingEvent> ParseICalEvents(string xml, DateTimeOffset from, DateTimeOffset to)
    {
        var events = new List<MeetingEvent>();
        var doc = XDocument.Parse(xml);
        XNamespace caldav = "urn:ietf:params:xml:ns:caldav";
        XNamespace dav = "DAV:";

        foreach (var response in doc.Descendants(dav + "response"))
        {
            var calendarData = response.Descendants(caldav + "calendar-data").FirstOrDefault();
            if (calendarData?.Value is not string ics || string.IsNullOrWhiteSpace(ics))
                continue;

            var uid = "";
            var summary = "";
            var organizer = "";
            DateTimeOffset? dtStart = null;
            DateTimeOffset? dtEnd = null;

            foreach (var line in ics.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("UID:", StringComparison.OrdinalIgnoreCase))
                    uid = trimmed[4..].Trim();
                else if (trimmed.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                    summary = UnescapeICal(trimmed[8..].Trim());
                else if (trimmed.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase))
                    organizer = ExtractOrganizerMail(trimmed);
                else if (trimmed.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
                    dtStart = ParseICalDateTime(trimmed);
                else if (trimmed.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
                    dtEnd = ParseICalDateTime(trimmed);
            }

            if (!string.IsNullOrEmpty(uid) && dtStart.HasValue && dtEnd.HasValue)
            {
                events.Add(new MeetingEvent
                {
                    Id = uid,
                    Summary = summary,
                    Organizer = organizer,
                    Start = dtStart.Value,
                    End = dtEnd.Value,
                    IsAllDay = false
                });
            }
        }

        return events.OrderBy(e => e.Start).ToList();
    }

    private static string UnescapeICal(string value)
    {
        return value
            .Replace("\\n", "\n")
            .Replace("\\;", ";")
            .Replace("\\,", ",")
            .Replace("\\\\", "\\");
    }

    private static string ExtractOrganizerMail(string line)
    {
        var mailtoIdx = line.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase);
        if (mailtoIdx >= 0)
        {
            var start = mailtoIdx + 7;
            var end = line.IndexOf(':', start);
            if (end < 0) end = line.Length;
            return line[start..end].Trim();
        }
        var colonIdx = line.IndexOf(':');
        return colonIdx >= 0 ? line[(colonIdx + 1)..].Trim() : "";
    }

    private static DateTimeOffset? ParseICalDateTime(string line)
    {
        var colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return null;
        var value = line[(colonIdx + 1)..].Trim();

        var isUtc = value.EndsWith("Z");
        var clean = value.Replace("Z", "").Replace("T", "");

        if (clean.Length >= 14 &&
            int.TryParse(clean[..4], out var y) &&
            int.TryParse(clean[4..6], out var mo) &&
            int.TryParse(clean[6..8], out var d) &&
            int.TryParse(clean[8..10], out var h) &&
            int.TryParse(clean[10..12], out var mi) &&
            int.TryParse(clean[12..14], out var s))
        {
            var dt = new DateTime(y, mo, d, h, mi, s, isUtc ? DateTimeKind.Utc : DateTimeKind.Local);
            return new DateTimeOffset(dt);
        }

        if (clean.Length >= 8 &&
            int.TryParse(clean[..4], out var yd) &&
            int.TryParse(clean[4..6], out var md) &&
            int.TryParse(clean[6..8], out var dd))
        {
            return new DateTimeOffset(new DateTime(yd, md, dd));
        }

        return null;
    }
}
