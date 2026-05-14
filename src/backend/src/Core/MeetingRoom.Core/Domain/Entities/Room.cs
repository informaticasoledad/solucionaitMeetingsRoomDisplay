using MeetingRoom.Core.Domain.ValueObjects;
using MeetingRoom.Core.Domain.Events;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Exceptions;

namespace MeetingRoom.Core.Domain.Entities;

public sealed class Room
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public int Capacity { get; private set; }
    public CalendarId CalendarId { get; private set; }
    public CalendarProvider Provider { get; private set; }
    public ClockMode ClockMode { get; private set; }
    public IReadOnlyList<MeetingEvent> CurrentEvents { get; private set; } = [];

    private Room() { }

    public Room(string id, string name, int capacity, CalendarId calendarId, CalendarProvider provider = CalendarProvider.Google, ClockMode clockMode = ClockMode.Digital)
    {
        SetId(id);
        SetName(name);
        SetCapacity(capacity);
        CalendarId = calendarId;
        Provider = provider;
        ClockMode = clockMode;
    }

    public void SetId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Room code is required.");
        if (id.Length > 8)
            throw new DomainException("Room code must be at most 8 characters.");
        Id = id.Trim().ToUpperInvariant();
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Room name is required.");
        Name = name.Trim();
    }

    public void SetCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new DomainException("Capacity must be greater than zero.");
        Capacity = capacity;
    }

    public void UpdateCalendar(CalendarId calendarId, CalendarProvider provider, ClockMode clockMode)
    {
        CalendarId = calendarId;
        Provider = provider;
        ClockMode = clockMode;
    }

    public void UpdateEvents(IEnumerable<MeetingEvent> events) =>
        CurrentEvents = events.ToList();

    public RoomStatus GetCurrentStatus(DateTimeOffset? now = null)
    {
        var n = now ?? DateTimeOffset.UtcNow;

        var current = CurrentEvents.FirstOrDefault(e => n >= e.Start && n < e.End);
        if (current is not null)
            return RoomStatus.Occupied;

        var next = CurrentEvents
            .Where(e => e.Start > n)
            .MinBy(e => e.Start);

        if (next is not null && (next.Start - n).TotalMinutes < 15)
            return RoomStatus.BusySoon;

        return RoomStatus.Free;
    }

    public MeetingEvent? GetCurrentMeeting(DateTimeOffset? now = null)
    {
        var n = now ?? DateTimeOffset.UtcNow;
        return CurrentEvents.FirstOrDefault(e => n >= e.Start && n < e.End);
    }

    public MeetingEvent? GetNextMeeting(DateTimeOffset? now = null)
    {
        var n = now ?? DateTimeOffset.UtcNow;
        return CurrentEvents.Where(e => e.Start > n).MinBy(e => e.Start);
    }
}
