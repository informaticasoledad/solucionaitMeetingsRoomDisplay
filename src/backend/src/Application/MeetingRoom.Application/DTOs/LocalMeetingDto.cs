namespace MeetingRoom.Application.DTOs;

public sealed record CreateMeetingRequest(string RoomId, string Title, string Organizer, DateTimeOffset Start, int DurationMinutes);

public sealed record LocalMeetingDto(Guid Id, string RoomId, string Title, string Organizer, DateTimeOffset Start, DateTimeOffset End);
