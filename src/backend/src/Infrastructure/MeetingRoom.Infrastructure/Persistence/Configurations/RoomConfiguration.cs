using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using MeetingRoom.Core.Domain.Entities;
using MeetingRoom.Core.Domain.Events;
using MeetingRoom.Core.Domain.Enums;
using MeetingRoom.Core.Domain.ValueObjects;

namespace MeetingRoom.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    private static readonly ValueComparer<IReadOnlyList<MeetingEvent>> EventsComparer = new(
        (a, b) => a == b || (a != null && b != null && a.SequenceEqual(b)),
        c => c.Aggregate(0, (hash, ev) => HashCode.Combine(hash, ev.GetHashCode())),
        c => c.ToList());

    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Capacity)
            .IsRequired();

        builder.Property(r => r.CalendarId)
            .IsRequired()
            .HasMaxLength(500)
            .HasConversion(
                calId => calId.Value,
                value => new CalendarId(value));

        builder.Property(r => r.Provider)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.ClockMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(r => r.CurrentEvents)
            .HasConversion(
                events => JsonSerializer.Serialize(events, JsonOptions),
                json => JsonSerializer.Deserialize<List<MeetingEvent>>(json, JsonOptions) ?? new List<MeetingEvent>())
            .Metadata.SetValueComparer(EventsComparer);
    }
}
