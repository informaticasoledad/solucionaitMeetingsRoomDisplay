namespace MeetingRoom.Tests.Domain;

using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Events;
using MeetingRoom.Core.Domain.ValueObjects;
using MeetingRoom.Core.Domain.Exceptions;

public class RoomTests
{
    private readonly CalendarId _calendarId = (CalendarId)"room@example.com";

    [Fact]
    public void CreateRoom_ValidParameters_CreatesSuccessfully()
    {
        var room = new Room("Sala A", 10, _calendarId);

        Assert.Equal("Sala A", room.Name);
        Assert.Equal(10, room.Capacity);
        Assert.Equal(_calendarId, room.CalendarId);
        Assert.Equal(CalendarProvider.Google, room.Provider);
        Assert.NotEqual(Guid.Empty, room.Id);
    }

    [Fact]
    public void CreateRoom_WithOffice365Provider_SetsProviderCorrectly()
    {
        var room = new Room("Sala B", 6, _calendarId, CalendarProvider.Office365);

        Assert.Equal(CalendarProvider.Office365, room.Provider);
    }

    [Fact]
    public void CreateRoom_EmptyName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Room("", 10, _calendarId));
    }

    [Fact]
    public void CreateRoom_ZeroCapacity_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Room("Sala A", 0, _calendarId));
    }

    [Fact]
    public void GetCurrentStatus_NoEvents_ReturnsFree()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var now = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero);

        var status = room.GetCurrentStatus(now);

        Assert.Equal(RoomStatus.Free, status);
    }

    [Fact]
    public void GetCurrentStatus_CurrentEvent_ReturnsOccupied()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var now = new DateTimeOffset(2026, 5, 13, 10, 30, 0, TimeSpan.Zero);

        var events = new List<MeetingEvent>
        {
            new()
            {
                Id = "1", Summary = "Reunion",
                Start = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero)
            }
        };
        room.UpdateEvents(events);

        var status = room.GetCurrentStatus(now);

        Assert.Equal(RoomStatus.Occupied, status);
    }

    [Fact]
    public void GetCurrentStatus_EventStartingIn5Minutes_ReturnsBusySoon()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var now = new DateTimeOffset(2026, 5, 13, 9, 57, 0, TimeSpan.Zero);

        var events = new List<MeetingEvent>
        {
            new()
            {
                Id = "1", Summary = "Reunion",
                Start = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero)
            }
        };
        room.UpdateEvents(events);

        var status = room.GetCurrentStatus(now);

        Assert.Equal(RoomStatus.BusySoon, status);
    }

    [Fact]
    public void GetCurrentStatus_EventStartingIn20Minutes_ReturnsFree()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var now = new DateTimeOffset(2026, 5, 13, 9, 40, 0, TimeSpan.Zero);

        var events = new List<MeetingEvent>
        {
            new()
            {
                Id = "1", Summary = "Reunion",
                Start = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero)
            }
        };
        room.UpdateEvents(events);

        var status = room.GetCurrentStatus(now);

        Assert.Equal(RoomStatus.Free, status);
    }

    [Fact]
    public void GetCurrentMeeting_WhenOccupied_ReturnsMeeting()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var now = new DateTimeOffset(2026, 5, 13, 10, 30, 0, TimeSpan.Zero);

        var events = new List<MeetingEvent>
        {
            new()
            {
                Id = "1", Summary = "Reunion Actual",
                Start = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero)
            }
        };
        room.UpdateEvents(events);

        var meeting = room.GetCurrentMeeting(now);

        Assert.NotNull(meeting);
        Assert.Equal("Reunion Actual", meeting.Summary);
    }

    [Fact]
    public void GetNextMeeting_ReturnsClosestUpcomingEvent()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var now = new DateTimeOffset(2026, 5, 13, 9, 0, 0, TimeSpan.Zero);

        var events = new List<MeetingEvent>
        {
            new()
            {
                Id = "1", Summary = "Evento 2",
                Start = new DateTimeOffset(2026, 5, 13, 10, 0, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 5, 13, 11, 0, 0, TimeSpan.Zero)
            },
            new()
            {
                Id = "2", Summary = "Evento 1",
                Start = new DateTimeOffset(2026, 5, 13, 9, 30, 0, TimeSpan.Zero),
                End = new DateTimeOffset(2026, 5, 13, 9, 45, 0, TimeSpan.Zero)
            }
        };
        room.UpdateEvents(events);

        var next = room.GetNextMeeting(now);

        Assert.NotNull(next);
        Assert.Equal("Evento 1", next.Summary);
    }

    [Fact]
    public void UpdateCalendar_SwitchesProviderAndCalendarId()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var newId = (CalendarId)"new@office365.com";

        room.UpdateCalendar(newId, CalendarProvider.Office365);

        Assert.Equal(newId, room.CalendarId);
        Assert.Equal(CalendarProvider.Office365, room.Provider);
    }

    [Fact]
    public void CalendarId_InvalidEmpty_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new CalendarId(""));
    }

    [Fact]
    public void UpdateEvents_ClonesList()
    {
        var room = new Room("Sala A", 10, _calendarId);
        var events = new List<MeetingEvent>
        {
            new() { Id = "1", Summary = "Test" }
        };

        room.UpdateEvents(events);
        events.Clear();

        Assert.Single(room.CurrentEvents);
    }
}
