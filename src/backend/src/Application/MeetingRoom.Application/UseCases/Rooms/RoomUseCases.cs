using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Exceptions;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Core.Domain.ValueObjects;
using MeetingRoom.Application.DTOs;

namespace MeetingRoom.Application.UseCases.Rooms;

public sealed class GetRooms(IRoomRepository repository)
{
    public async Task<IReadOnlyList<RoomDto>> HandleAsync(CancellationToken ct = default)
    {
        var rooms = await repository.GetAllAsync(ct);
        return rooms.Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.CalendarId, r.Provider.ToString(), r.ClockMode.ToString())).ToList();
    }
}

public sealed class GetRoomById(IRoomRepository repository)
{
    public async Task<RoomDto?> HandleAsync(string id, CancellationToken ct = default)
    {
        var room = await repository.GetByIdAsync(id, ct);
        return room is null ? null : new RoomDto(room.Id, room.Name, room.Capacity, room.CalendarId, room.Provider.ToString(), room.ClockMode.ToString());
    }
}

public sealed class CreateRoom(IRoomRepository repository)
{
    public async Task<RoomDto> HandleAsync(CreateRoomRequest request, CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(request.Id, ct);
        if (existing is not null)
            throw new DomainException($"Ya existe una sala con el código '{request.Id}'.");

        var provider = Enum.Parse<CalendarProvider>(request.Provider, ignoreCase: true);
        var clockMode = Enum.Parse<ClockMode>(request.ClockMode, ignoreCase: true);
        var calendarId = provider == CalendarProvider.Local ? request.Id : request.CalendarId;
        var room = new Room(request.Id, request.Name, request.Capacity, (CalendarId)calendarId, provider, clockMode);
        await repository.AddAsync(room, ct);
        return new RoomDto(room.Id, room.Name, room.Capacity, room.CalendarId, room.Provider.ToString(), room.ClockMode.ToString());
    }
}

public sealed class UpdateRoom(IRoomRepository repository)
{
    public async Task<RoomDto?> HandleAsync(string id, UpdateRoomRequest request, CancellationToken ct = default)
    {
        var room = await repository.GetByIdAsync(id, ct);
        if (room is null) return null;

        var provider = Enum.Parse<CalendarProvider>(request.Provider, ignoreCase: true);
        var clockMode = Enum.Parse<ClockMode>(request.ClockMode, ignoreCase: true);
        room.SetName(request.Name);
        room.SetCapacity(request.Capacity);
        room.UpdateCalendar((CalendarId)request.CalendarId, provider, clockMode);

        await repository.UpdateAsync(room, ct);
        return new RoomDto(room.Id, room.Name, room.Capacity, room.CalendarId, room.Provider.ToString(), room.ClockMode.ToString());
    }
}

public sealed class DeleteRoom(IRoomRepository repository)
{
    public async Task<bool> HandleAsync(string id, CancellationToken ct = default)
    {
        var room = await repository.GetByIdAsync(id, ct);
        if (room is null) return false;

        await repository.DeleteAsync(room, ct);
        return true;
    }
}
