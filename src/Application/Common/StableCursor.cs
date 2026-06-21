using System.Text;
using System.Text.Json;

namespace Application.Common;

public readonly record struct StableCursor(DateTimeOffset Timestamp, Guid Id)
{
    public string Encode()
    {
        var json = JsonSerializer.Serialize(new CursorPayload(Timestamp, Id));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static StableCursor? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            var payload = JsonSerializer.Deserialize<CursorPayload>(json);
            if (payload is null || payload.Id == Guid.Empty)
                throw new FormatException();
            return new StableCursor(payload.Timestamp, payload.Id);
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new ArgumentException("Invalid pagination cursor.", nameof(value));
        }
    }

    private sealed record CursorPayload(DateTimeOffset Timestamp, Guid Id);
}
