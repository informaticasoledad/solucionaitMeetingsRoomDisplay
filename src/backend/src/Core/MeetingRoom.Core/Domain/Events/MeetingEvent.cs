namespace MeetingRoom.Core.Domain.Events;

public sealed record MeetingEvent
{
    public string Id { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Organizer { get; init; } = string.Empty;
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public bool IsAllDay { get; init; }
}
