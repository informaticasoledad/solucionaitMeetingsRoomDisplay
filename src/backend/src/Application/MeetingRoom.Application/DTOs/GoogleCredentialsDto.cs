namespace MeetingRoom.Application.DTOs;

public sealed record GoogleCredentialsDto(string CredentialsJson, string Provider = "Google");

public sealed record CalendarStatusDto(bool IsConfigured);
