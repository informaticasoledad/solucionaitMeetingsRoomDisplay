using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace MeetingRoom.Infrastructure.Services;

public class GoogleCalendarProvider : ICalendarProvider, IDisposable
{
    private CalendarService? _service;

    public Task InitializeAsync(string credentialsJson, CancellationToken ct = default)
    {
        var credential = GoogleCredential.FromJson(credentialsJson)
            .CreateScoped(CalendarService.Scope.CalendarEvents)
            .CreateWithUser("meeting-room-display@solucionait.iam.gserviceaccount.com");

        _service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "MeetingRoomDisplay"
        });

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(
        string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = _service!.Events.List(calendarId);
        request.TimeMinDateTimeOffset = from;
        request.TimeMaxDateTimeOffset = to;
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 50;

        var events = await request.ExecuteAsync(ct);
        return MapEvents(events.Items ?? []);
    }

    public async Task<MeetingEvent> CreateQuickEventAsync(
        string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default)
    {
        EnsureInitialized();
        var googleEvent = new Event
        {
            Summary = summary,
            Start = new EventDateTime { DateTimeDateTimeOffset = start, TimeZone = "UTC" },
            End = new EventDateTime { DateTimeDateTimeOffset = start.AddMinutes(durationMinutes), TimeZone = "UTC" }
        };

        var created = await _service!.Events.Insert(googleEvent, calendarId).ExecuteAsync(ct);
        return new MeetingEvent
        {
            Id = created.Id ?? string.Empty,
            Summary = created.Summary ?? string.Empty,
            Organizer = created.Organizer?.Email ?? string.Empty,
            Start = created.Start?.DateTimeDateTimeOffset ?? start,
            End = created.End?.DateTimeDateTimeOffset ?? start.AddMinutes(durationMinutes),
            IsAllDay = false
        };
    }

    public void Dispose() => _service?.Dispose();

    private void EnsureInitialized()
    {
        if (_service is null)
            throw new InvalidOperationException("Google Calendar not configured.");
    }

    private static IReadOnlyList<MeetingEvent> MapEvents(IList<Event> items)
    {
        return items.Select(e =>
        {
            var start = e.Start?.DateTimeDateTimeOffset
                ?? (e.Start?.Date is not null ? DateTimeOffset.Parse(e.Start.Date) : DateTimeOffset.MinValue);
            var end = e.End?.DateTimeDateTimeOffset
                ?? (e.End?.Date is not null ? DateTimeOffset.Parse(e.End.Date) : DateTimeOffset.MinValue);
            return new MeetingEvent
            {
                Id = e.Id ?? string.Empty,
                Summary = e.Summary ?? string.Empty,
                Organizer = e.Organizer?.Email ?? e.Organizer?.DisplayName ?? string.Empty,
                Start = start,
                End = end,
                IsAllDay = e.Start?.Date is not null
            };
        }).ToList();
    }
}
