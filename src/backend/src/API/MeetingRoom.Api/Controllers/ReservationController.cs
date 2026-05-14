namespace MeetingRoom.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MeetingRoom.Application.DTOs;
using MeetingRoom.Application.UseCases.Reservation;

[ApiController]
[Route("api/reservations")]
public class ReservationController(QuickReserve quickReserve) : ControllerBase
{
    [HttpPost("quick")]
    public async Task<IActionResult> QuickReserve([FromBody] QuickReserveRequest request, CancellationToken ct)
    {
        var result = await quickReserve.HandleAsync(request, ct);
        return Ok(result);
    }
}
