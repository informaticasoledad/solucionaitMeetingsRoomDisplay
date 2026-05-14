namespace MeetingRoom.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MeetingRoom.Application.DTOs;
using MeetingRoom.Application.UseCases.Rooms;

[ApiController]
[Route("api/rooms")]
public class RoomsController(
    GetRooms getRooms,
    GetRoomById getRoomById,
    CreateRoom createRoom,
    UpdateRoom updateRoom,
    DeleteRoom deleteRoom) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var rooms = await getRooms.HandleAsync(ct);
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var room = await getRoomById.HandleAsync(id, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoomRequest request, CancellationToken ct)
    {
        var room = await createRoom.HandleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateRoomRequest request, CancellationToken ct)
    {
        var room = await updateRoom.HandleAsync(id, request, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await deleteRoom.HandleAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
