using MeetingRoom.Core.Domain.Enums;

namespace MeetingRoom.Core.Domain.Interfaces;

public interface ICalendarProviderFactory
{
    ICalendarProvider GetProvider(CalendarProvider provider);
    Task InitializeProviderAsync(CalendarProvider provider, string credentialsJson, CancellationToken ct = default);
}
