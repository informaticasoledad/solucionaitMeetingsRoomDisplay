using Microsoft.EntityFrameworkCore;
using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Infrastructure.Persistence;

namespace MeetingRoom.Infrastructure.Repositories;

public class RoomRepository(AppDbContext context) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.Rooms.FindAsync([id], ct);
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Rooms.AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(Room room, CancellationToken ct = default)
    {
        context.Rooms.Add(room);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        context.Rooms.Update(room);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Room room, CancellationToken ct = default)
    {
        context.Rooms.Remove(room);
        await context.SaveChangesAsync(ct);
    }
}
