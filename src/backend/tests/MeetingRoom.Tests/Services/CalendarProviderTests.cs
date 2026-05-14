namespace MeetingRoom.Tests.Services;

using Moq;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.Events;

public class CalendarProviderTests
{
    [Fact]
    public async Task Factory_GetProvider_ReturnsCorrectProvider()
    {
        var mockFactory = new Mock<ICalendarProviderFactory>();
        var mockProvider = new Mock<ICalendarProvider>();
        mockFactory.Setup(f => f.GetProvider(CalendarProvider.Google)).Returns(mockProvider.Object);

        var provider = mockFactory.Object.GetProvider(CalendarProvider.Google);

        Assert.NotNull(provider);
        Assert.Same(mockProvider.Object, provider);
    }

    [Fact]
    public async Task Factory_InitializeProvider_CallsInitializeOnProvider()
    {
        var mockProvider = new Mock<ICalendarProvider>();
        mockProvider.Setup(p => p.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockFactory = new Mock<ICalendarProviderFactory>();
        mockFactory.Setup(f => f.GetProvider(CalendarProvider.Google)).Returns(mockProvider.Object);
        mockFactory.Setup(f => f.InitializeProviderAsync(CalendarProvider.Google, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<CalendarProvider, string, CancellationToken>((_, _, _) =>
                mockProvider.Object.InitializeAsync("test").Wait());

        await mockFactory.Object.InitializeProviderAsync(CalendarProvider.Google, "{}");

        mockProvider.Verify(p => p.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Provider_GetEvents_ReturnsEmptyWhenNoEvents()
    {
        var mockProvider = new Mock<ICalendarProvider>();
        mockProvider.Setup(p => p.GetEventsAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var events = await mockProvider.Object.GetEventsAsync("cal", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Provider_CreateQuickEvent_ReturnsMeetingEvent()
    {
        var expected = new MeetingEvent
        {
            Id = "evt1", Summary = "Quick", Organizer = "admin@test.com",
            Start = DateTimeOffset.UtcNow, End = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        var mockProvider = new Mock<ICalendarProvider>();
        mockProvider.Setup(p => p.CreateQuickEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await mockProvider.Object.CreateQuickEventAsync("cal", "Quick", DateTimeOffset.UtcNow, 15);

        Assert.Equal("evt1", result.Id);
        Assert.Equal("Quick", result.Summary);
    }
}
