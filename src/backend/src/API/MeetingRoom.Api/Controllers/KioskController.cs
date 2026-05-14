namespace MeetingRoom.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MeetingRoom.Application.UseCases.Rooms;

[ApiController]
[Route("api/kiosk")]
public class KioskController : ControllerBase
{
    private readonly string _pin;
    private readonly GetRoomById _getRoomById;

    public KioskController(IConfiguration configuration, GetRoomById getRoomById)
    {
        _pin = configuration["KioskPin"] ?? "123456";
        _getRoomById = getRoomById;
    }

    [HttpPost("validate-pin")]
    public IActionResult ValidatePin([FromBody] ValidatePinRequest request)
    {
        if (request.Pin == _pin)
            return Ok(new { valid = true });

        return Unauthorized(new { valid = false, error = "PIN incorrecto" });
    }

    [HttpPost("validate-room")]
    public async Task<IActionResult> ValidateRoom([FromBody] ValidateRoomRequest request, CancellationToken ct)
    {
        if (request.Pin != _pin)
            return Unauthorized(new { valid = false, error = "PIN incorrecto" });

        var room = await _getRoomById.HandleAsync(request.RoomId, ct);
        return room is not null
            ? Ok(new { valid = true, room = new { room.Id, room.Name, room.Capacity } })
            : NotFound(new { valid = false, error = "La sala no existe" });
    }

    public record ValidatePinRequest(string Pin);
    public record ValidateRoomRequest(string Pin, string RoomId);
}
