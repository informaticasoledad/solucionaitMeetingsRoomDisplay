using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingRoom.Infrastructure.Services;

public class ICalCalendarProvider : ICalendarProvider, IDisposable
{
    private readonly HttpClient _client = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private bool _initialized;
    private string _subscriptionUrl = string.Empty;

    public ICalCalendarProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task InitializeAsync(string credentialsJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(credentialsJson);
        var root = doc.RootElement;
        _subscriptionUrl = root.GetProperty("url").GetString()!;
        _initialized = true;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(
        string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        EnsureInitialized();

        var url = string.IsNullOrEmpty(calendarId) ? _subscriptionUrl : calendarId;
        var ics = await _client.GetStringAsync(url, ct);
        return ParseICalEvents(ics, from, to);
    }

    public async Task<MeetingEvent> CreateQuickEventAsync(
        string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default)
    {
        EnsureInitialized();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();

        var subscriptionId = string.IsNullOrEmpty(calendarId) ? _subscriptionUrl : calendarId;
        var end = start.AddMinutes(durationMinutes);

        var existing = await db.Set<Core.Domain.Entities.Room>()
            .FirstOrDefaultAsync(r => r.CalendarId == subscriptionId, ct);

        var roomId = existing?.Id ?? Guid.NewGuid().ToString("N")[..8];
        var meeting = new Core.Domain.Entities.LocalMeeting(roomId, summary, "Tablet Kiosko", start, end, subscriptionId);

        db.Set<Core.Domain.Entities.LocalMeeting>().Add(meeting);
        await db.SaveChangesAsync(ct);

        return new MeetingEvent
        {
            Id = meeting.Id.ToString(),
            Summary = meeting.Title,
            Organizer = meeting.Organizer,
            Start = meeting.Start,
            End = meeting.End,
            IsAllDay = false
        };
    }

    public void Dispose() => _client.Dispose();

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("iCal subscription not configured.");
    }

    private static string ExtractLineValue(ReadOnlySpan<char> line, ReadOnlySpan<char> prefix)
    {
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return "";
        var value = line[prefix.Length..].Trim();
        return UnescapeICal(value.ToString());
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

    private static IReadOnlyList<MeetingEvent> ParseICalEvents(string ics, DateTimeOffset from, DateTimeOffset to)
    {
        var events = new List<MeetingEvent>();
        var lines = ics.Replace("\r\n", "\n").Split('\n');

        string? uid = null, summary = null, organizer = null;
        DateTimeOffset? dtStart = null, dtEnd = null;
        var inEvent = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.AsSpan().Trim();

            if (line.StartsWith("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                inEvent = true;
                uid = summary = organizer = null;
                dtStart = dtEnd = null;
                continue;
            }

            if (!inEvent) continue;

            if (line.StartsWith("END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(uid) && dtStart.HasValue && dtEnd.HasValue
                    && dtStart.Value < to && dtEnd.Value > from)
                {
                    events.Add(new MeetingEvent
                    {
                        Id = uid!,
                        Summary = summary ?? "",
                        Organizer = organizer ?? "",
                        Start = dtStart.Value,
                        End = dtEnd.Value,
                        IsAllDay = false
                    });
                }
                inEvent = false;
                continue;
            }

            if (line.StartsWith("UID:", StringComparison.OrdinalIgnoreCase))
                uid = ExtractLineValue(line, "UID:");
            else if (line.StartsWith("SUMMARY:", StringComparison.OrdinalIgnoreCase))
                summary = ExtractLineValue(line, "SUMMARY:");
            else if (line.StartsWith("ORGANIZER", StringComparison.OrdinalIgnoreCase))
                organizer = ExtractOrganizerMail(line.ToString());
            else if (line.StartsWith("DTSTART", StringComparison.OrdinalIgnoreCase))
                dtStart = ParseICalDateTime(line.ToString());
            else if (line.StartsWith("DTEND", StringComparison.OrdinalIgnoreCase))
                dtEnd = ParseICalDateTime(line.ToString());
        }

        return events.OrderBy(e => e.Start).ToList();
    }
}
