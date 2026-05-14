using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingRoom.Infrastructure.Services;

public class LocalCalendarProvider : ICalendarProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LocalCalendarProvider(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task InitializeAsync(string credentialsJson, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<IReadOnlyList<MeetingEvent>> GetEventsAsync(string calendarId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();

        var meetings = await db.Set<Core.Domain.Entities.LocalMeeting>()
            .Where(m => m.CalendarId == calendarId)
            .ToListAsync(ct);

        return meetings
            .Where(m => m.Start < to && m.End > from)
            .OrderBy(m => m.Start)
            .Select(m => new MeetingEvent
        {
            Id = m.Id.ToString(),
            Summary = m.Title,
            Organizer = m.Organizer,
            Start = m.Start,
            End = m.End,
            IsAllDay = false
        }).ToList();
    }

    public async Task<MeetingEvent> CreateQuickEventAsync(string calendarId, string summary, DateTimeOffset start, int durationMinutes, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Persistence.AppDbContext>();

        var room = await db.Set<Core.Domain.Entities.Room>().FindAsync([calendarId], ct);
        var roomId = room?.Id ?? calendarId;

        var meeting = new Core.Domain.Entities.LocalMeeting(roomId, summary, "Tablet Kiosko", start, start.AddMinutes(durationMinutes), calendarId);
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
}
