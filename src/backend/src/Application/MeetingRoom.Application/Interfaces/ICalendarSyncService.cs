namespace MeetingRoom.Application.Interfaces;

public interface ICalendarSyncService
{
    Task SyncAllAsync(CancellationToken ct = default);
    Task SyncRoomAsync(Guid roomId, CancellationToken ct = default);
}
