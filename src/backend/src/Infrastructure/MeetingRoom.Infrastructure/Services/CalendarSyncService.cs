using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MeetingRoom.Core.Domain.Interfaces;

namespace MeetingRoom.Infrastructure.Services;

public class CalendarSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<CalendarSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncAllRooms(scopeFactory, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    public async Task SyncAllRooms(IServiceScopeFactory sf, CancellationToken ct)
    {
        try
        {
            using var scope = sf.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
            var providerFactory = scope.ServiceProvider.GetRequiredService<ICalendarProviderFactory>();

            var rooms = await repository.GetAllAsync(ct);
            var now = DateTimeOffset.UtcNow;
            var rangeEnd = now.AddDays(7);

            foreach (var room in rooms)
            {
                try
                {
                    var provider = providerFactory.GetProvider(room.Provider);
                    var events = await provider.GetEventsAsync(room.CalendarId, now, rangeEnd, ct);
                    room.UpdateEvents(events);
                    await repository.UpdateAsync(room, ct);
                }
                catch (InvalidOperationException)
                {
                    logger.LogWarning("Provider {Provider} not configured for room {RoomName}", room.Provider, room.Name);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to sync events for room {RoomName} ({RoomId})", room.Name, room.Id);
                }
            }

            logger.LogDebug("Calendar sync completed at {Time}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Calendar sync cycle failed");
        }
    }

    public async Task SyncSingleRoom(IServiceScopeFactory sf, string roomId, CancellationToken ct)
    {
        using var scope = sf.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<ICalendarProviderFactory>();

        var room = await repository.GetByIdAsync(roomId, ct);
        if (room is null) return;

        var provider = providerFactory.GetProvider(room.Provider);
        var now = DateTimeOffset.UtcNow;
        var events = await provider.GetEventsAsync(room.CalendarId, now, now.AddDays(7), ct);
        room.UpdateEvents(events);
        await repository.UpdateAsync(room, ct);
    }
}

public class ManualCalendarSyncService(IServiceScopeFactory scopeFactory, CalendarSyncService syncService)
    : MeetingRoom.Application.Interfaces.ICalendarSyncService
{
    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        await syncService.SyncAllRooms(scopeFactory, ct);
    }

    public async Task SyncRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        await syncService.SyncSingleRoom(scopeFactory, roomId.ToString(), ct);
    }
}
