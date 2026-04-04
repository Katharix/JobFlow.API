using System.Text;

namespace JobFlow.Business.Utilities;

public static class CursorToken
{
    public static string Build(DateTime timestampUtc, Guid id)
    {
        var raw = $"{timestampUtc.ToUniversalTime().Ticks}|{id:D}";
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
}
