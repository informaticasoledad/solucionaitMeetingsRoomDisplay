using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Application.DTOs;

namespace MeetingRoom.Application.UseCases.Status;

public sealed class GetAllRoomStatuses(IRoomRepository repository)
{
    public async Task<IReadOnlyList<RoomStatusDto>> HandleAsync(CancellationToken ct = default)
    {
        var rooms = await repository.GetAllAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return rooms.Select(r => MapToDto(r, now)).ToList();
    }

    private static RoomStatusDto MapToDto(Room room, DateTimeOffset now)
    {
        var status = room.GetCurrentStatus(now);
        var current = room.GetCurrentMeeting(now);
        var next = room.GetNextMeeting(now);
        var nextAvailable = current?.End ?? now;

        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var todaysEvents = room.CurrentEvents
            .Where(e => e.Start >= todayStart && e.Start < todayEnd)
            .OrderBy(e => e.Start)
            .Select(e => new MeetingEventDto(e.Id, e.Summary, e.Organizer, e.Start, e.End, e.IsAllDay))
            .ToList();

        return new RoomStatusDto(
            room.Id, room.Name, room.Capacity, status.ToString(), room.ClockMode.ToString(),
            current is null ? null : new MeetingEventDto(current.Id, current.Summary, current.Organizer, current.Start, current.End, current.IsAllDay),
            next is null ? null : new MeetingEventDto(next.Id, next.Summary, next.Organizer, next.Start, next.End, next.IsAllDay),
            nextAvailable, todaysEvents);
    }
}

public sealed class GetRoomStatus(IRoomRepository repository)
{
    public async Task<RoomStatusDto?> HandleAsync(string roomId, CancellationToken ct = default)
    {
        var room = await repository.GetByIdAsync(roomId, ct);
        if (room is null) return null;

        var now = DateTimeOffset.UtcNow;
        var status = room.GetCurrentStatus(now);
        var current = room.GetCurrentMeeting(now);
        var next = room.GetNextMeeting(now);
        var nextAvailable = current?.End ?? now;

        var todayStart = now.Date;
        var todayEnd = todayStart.AddDays(1);
        var todaysEvents = room.CurrentEvents
            .Where(e => e.Start >= todayStart && e.Start < todayEnd)
            .OrderBy(e => e.Start)
            .Select(e => new MeetingEventDto(e.Id, e.Summary, e.Organizer, e.Start, e.End, e.IsAllDay))
            .ToList();

        return new RoomStatusDto(
            room.Id, room.Name, room.Capacity, status.ToString(), room.ClockMode.ToString(),
            current is null ? null : new MeetingEventDto(current.Id, current.Summary, current.Organizer, current.Start, current.End, current.IsAllDay),
            next is null ? null : new MeetingEventDto(next.Id, next.Summary, next.Organizer, next.Start, next.End, next.IsAllDay),
            nextAvailable, todaysEvents);
    }
}
