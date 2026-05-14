using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.Exceptions;
using MeetingRoom.Core.Domain.Interfaces;
using MeetingRoom.Application.DTOs;

namespace MeetingRoom.Application.UseCases.Reservation;

public sealed class QuickReserve(IRoomRepository repository, ICalendarProviderFactory providerFactory)
{
    public async Task<MeetingEventDto> HandleAsync(QuickReserveRequest request, CancellationToken ct = default)
    {
        var room = await repository.GetByIdAsync(request.RoomId, ct)
            ?? throw new DomainException("Room not found.");

        var status = room.GetCurrentStatus();
        if (status == RoomStatus.Occupied)
            throw new DomainException("Room is currently occupied.");

        var provider = providerFactory.GetProvider(room.Provider);
        var start = request.StartTime ?? DateTimeOffset.UtcNow;
        if (start <= DateTimeOffset.UtcNow) start = DateTimeOffset.UtcNow.AddMinutes(1);
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"Reserva — {room.Name}"
            : request.Title;
        var organizer = string.IsNullOrWhiteSpace(request.OrganizerName)
            ? "Tablet Kiosko"
            : request.OrganizerName;
        var quickEvent = await provider.CreateQuickEventAsync(
            room.CalendarId,
            title,
            start,
            request.DurationMinutes,
            ct);

        room.UpdateEvents(room.CurrentEvents.Append(quickEvent));
        await repository.UpdateAsync(room, ct);

        return new MeetingEventDto(quickEvent.Id, quickEvent.Summary, quickEvent.Organizer, quickEvent.Start, quickEvent.End, quickEvent.IsAllDay);
    }
}
