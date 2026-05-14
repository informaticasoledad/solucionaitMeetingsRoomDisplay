using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MeetingRoom.Infrastructure.Persistence;
using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Events;
using MeetingRoom.Application.DTOs;

namespace MeetingRoom.Api.Controllers;

[ApiController]
[Route("api/meetings")]
public class MeetingsController(AppDbContext db, IServiceScopeFactory scopeFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? roomId, CancellationToken ct)
    {
        var query = db.LocalMeetings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(roomId))
            query = query.Where(m => m.RoomId == roomId);

        var meetings = await query
            .Select(m => new { m.Id, m.RoomId, m.Title, m.Organizer, m.Start, m.End })
            .ToListAsync(ct);

        var result = meetings
            .OrderBy(m => m.Start)
            .Select(m => new LocalMeetingDto(m.Id, m.RoomId, m.Title, m.Organizer, m.Start, m.End))
            .ToList();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeetingRequest request, CancellationToken ct)
    {
        var room = await db.Rooms.FindAsync([request.RoomId], ct);
        var calendarId = room?.CalendarId?.ToString() ?? request.RoomId;

        var end = request.Start.AddMinutes(request.DurationMinutes);
        var meeting = new LocalMeeting(request.RoomId, request.Title, request.Organizer, request.Start, end, calendarId);
        db.LocalMeetings.Add(meeting);
        await db.SaveChangesAsync(ct);

        if (room is not null)
        {
            await SyncRoomEvents(db, room, ct);
        }

        return Ok(new LocalMeetingDto(meeting.Id, meeting.RoomId, meeting.Title, meeting.Organizer, meeting.Start, meeting.End));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var meeting = await db.LocalMeetings.FindAsync([id], ct);
        if (meeting is null) return NotFound();

        var roomId = meeting.RoomId;
        db.LocalMeetings.Remove(meeting);
        await db.SaveChangesAsync(ct);

        var room = await db.Rooms.FindAsync([roomId], ct);
        if (room is not null)
        {
            await SyncRoomEvents(db, room, ct);
        }

        return NoContent();
    }

    private static async Task SyncRoomEvents(AppDbContext db, Room room, CancellationToken ct)
    {
        var calendarId = room.CalendarId?.ToString() ?? room.Id;
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(7);

        var meetings = await db.LocalMeetings
            .Where(m => m.CalendarId == calendarId)
            .ToListAsync(ct);

        var filteredMeetings = meetings
            .Where(m => m.Start < to && m.End > from)
            .OrderBy(m => m.Start)
            .ToList();

        var events = filteredMeetings
            .Select(m => new MeetingEvent
            {
                Id = m.Id.ToString(),
                Summary = m.Title,
                Organizer = m.Organizer,
                Start = m.Start,
                End = m.End,
                IsAllDay = false
            })
            .ToList();

        room.UpdateEvents(events);
        db.Rooms.Update(room);
        await db.SaveChangesAsync(ct);
    }
}
