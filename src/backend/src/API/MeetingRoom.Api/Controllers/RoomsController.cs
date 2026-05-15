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

    [HttpPost("test-ical-url")]
    public async Task<IActionResult> TestICalUrl([FromBody] TestICalRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest(new { valid = false, error = "La URL no puede estar vacía." });

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            var response = await client.GetAsync(request.Url, ct);
            if (!response.IsSuccessStatusCode)
                return Ok(new { valid = false, error = $"Error HTTP {(int)response.StatusCode} al acceder a la URL." });

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!body.Contains("BEGIN:VCALENDAR", StringComparison.OrdinalIgnoreCase))
                return Ok(new { valid = false, error = "La URL no devuelve un calendario iCal válido." });

            var eventCount = body.Split("BEGIN:VEVENT", StringSplitOptions.None).Length - 1;
            return Ok(new { valid = true, message = $"Conexión exitosa. Se encontraron {eventCount} eventos.", eventCount });
        }
        catch (TaskCanceledException)
        {
            return Ok(new { valid = false, error = "Timeout. La URL no respondió en 10 segundos." });
        }
        catch (HttpRequestException ex)
        {
            return Ok(new { valid = false, error = $"Error de conexión: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return Ok(new { valid = false, error = $"Error inesperado: {ex.Message}" });
        }
    }

    public record TestICalRequest(string Url);
}
