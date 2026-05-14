using MeetingRoom.Application.Interfaces;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Application.DTOs;

namespace MeetingRoom.Application.UseCases.Calendar;

public sealed class SyncCalendars(IRoomRepository repository, ICalendarSyncService syncService)
{
    public async Task HandleAsync(CancellationToken ct = default)
    {
        await syncService.SyncAllAsync(ct);
    }
}

public sealed class ConfigureProvider(ICalendarProviderFactory providerFactory)
{
    public async Task HandleAsync(GoogleCredentialsDto request, CancellationToken ct = default)
    {
        var provider = Enum.Parse<CalendarProvider>(request.Provider, ignoreCase: true);
        await providerFactory.InitializeProviderAsync(provider, request.CredentialsJson, ct);
    }
}
