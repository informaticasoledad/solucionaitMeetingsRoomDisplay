namespace MeetingRoom.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MeetingRoom.Application.UseCases.Status;

[ApiController]
[Route("api/status")]
public class StatusController(
    GetAllRoomStatuses getAllRoomStatuses,
    GetRoomStatus getRoomStatus) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var statuses = await getAllRoomStatuses.HandleAsync(ct);
        return Ok(statuses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var status = await getRoomStatus.HandleAsync(id, ct);
        return status is null ? NotFound() : Ok(status);
    }
}
