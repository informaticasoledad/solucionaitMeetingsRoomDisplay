using MeetingRoom.Core.Domain.Entities;

namespace MeetingRoom.Core.Domain.Interfaces;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Room room, CancellationToken ct = default);
    Task UpdateAsync(Room room, CancellationToken ct = default);
    Task DeleteAsync(Room room, CancellationToken ct = default);
}
