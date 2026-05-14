namespace MeetingRoom.Core.Domain.ValueObjects;

public sealed record CalendarId
{
    public string Value { get; }

    public CalendarId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CalendarId cannot be empty.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(CalendarId id) => id.Value;
    public static explicit operator CalendarId(string value) => new(value);
}
