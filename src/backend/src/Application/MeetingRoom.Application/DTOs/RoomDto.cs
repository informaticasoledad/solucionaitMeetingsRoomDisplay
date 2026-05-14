namespace MeetingRoom.Application.DTOs;

public sealed record RoomDto(string Id, string Name, int Capacity, string CalendarId, string Provider, string ClockMode);

public sealed record CreateRoomRequest(string Id, string Name, int Capacity, string CalendarId, string Provider = "Google", string ClockMode = "Digital");

public sealed record UpdateRoomRequest(string Name, int Capacity, string CalendarId, string Provider = "Google", string ClockMode = "Digital");
