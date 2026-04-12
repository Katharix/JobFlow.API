using System.Text;

namespace JobFlow.Business.Utilities;

public static class CursorToken
{
    public static string Build(DateTime timestampUtc, Guid id)
    {
        var raw = $"{timestampUtc.ToUniversalTime().Ticks}|{id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static string BuildOffset(int offset)
    {
        var raw = $"off|{offset}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryRead(string? cursor, out DateTime timestampUtc, out Guid id)
    {
        timestampUtc = default;
        id = Guid.Empty;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));

            // Skip offset-style cursors
            if (decoded.StartsWith("off|"))
                return false;

            var split = decoded.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
                return false;

            if (!long.TryParse(split[0], out var ticks))
                return false;

            if (!Guid.TryParse(split[1], out id))
                return false;

            timestampUtc = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryReadOffset(string? cursor, out int offset)
    {
        offset = 0;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            if (!decoded.StartsWith("off|"))
                return false;

            return int.TryParse(decoded.AsSpan(4), out offset) && offset >= 0;
        }
        catch
        {
            return false;
        }
    }
}
