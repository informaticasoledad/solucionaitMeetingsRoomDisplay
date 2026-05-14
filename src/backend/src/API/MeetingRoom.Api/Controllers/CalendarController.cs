namespace MeetingRoom.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MeetingRoom.Application.DTOs;
using MeetingRoom.Application.UseCases.Calendar;
using MeetingRoom.Core.Domain.Interfaces;

[ApiController]
[Route("api/calendar")]
public class CalendarController(
    ICalendarProviderFactory providerFactory,
    ConfigureProvider configureProvider,
    SyncCalendars syncCalendars) : ControllerBase
{
    [HttpPost("credentials")]
    public async Task<IActionResult> SetCredentials([FromBody] GoogleCredentialsDto request, CancellationToken ct)
    {
        await configureProvider.HandleAsync(request, ct);
        return Ok(new { message = "Credentials configured successfully" });
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        await syncCalendars.HandleAsync(ct);
        return Ok(new { message = "Sync completed" });
    }
}
