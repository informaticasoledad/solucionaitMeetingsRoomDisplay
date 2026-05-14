namespace MeetingRoom.Core.Domain.Entities;

public sealed class LocalMeeting
{
    public Guid Id { get; private set; }
    public string RoomId { get; private set; }
    public string Title { get; private set; }
    public string Organizer { get; private set; }
    public DateTimeOffset Start { get; private set; }
    public DateTimeOffset End { get; private set; }
    public string? CalendarId { get; private set; }

    private LocalMeeting() { }

    public LocalMeeting(string roomId, string title, string organizer, DateTimeOffset start, DateTimeOffset end, string? calendarId = null)
    {
        Id = Guid.CreateVersion7();
        RoomId = roomId;
        Title = title;
        Organizer = organizer;
        Start = start;
        End = end;
        CalendarId = calendarId;
    }
}
