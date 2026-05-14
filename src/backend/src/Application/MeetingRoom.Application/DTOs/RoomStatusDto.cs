namespace MeetingRoom.Application.DTOs;

public sealed record RoomStatusDto(
    string RoomId,
    string RoomName,
    int Capacity,
    string Status,
    string ClockMode,
    MeetingEventDto? CurrentMeeting,
    MeetingEventDto? NextMeeting,
    DateTimeOffset? NextAvailableAt,
    IReadOnlyList<MeetingEventDto> TodaysEvents);

public sealed record MeetingEventDto(
    string Id,
    string Summary,
    string Organizer,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay);

public sealed record QuickReserveRequest(
    string RoomId,
    int DurationMinutes = 30,
    string Title = "",
    string OrganizerName = "",
    DateTimeOffset? StartTime = null);
