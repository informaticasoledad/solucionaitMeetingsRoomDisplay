using MeetingRoom.Core.Domain.Events;

namespace MeetingRoom.Core.Domain.Interfaces;

public interface ICalendarProvider
{
    Task InitializeAsync(string credentialsJson, CancellationToken ct = default);
    Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<MeetingEvent> CreateQuickEventAsync(string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default);
}

public interface IGoogleCalendarService : ICalendarProvider { }
