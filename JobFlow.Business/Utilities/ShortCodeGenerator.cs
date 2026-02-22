using System.Security.Cryptography;
using System.Text;

namespace JobFlow.Business.Utilities;

public static class ShortCodeGenerator
{
    public static string Generate(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        var sb = new StringBuilder(length);

        foreach (var b in bytes)
            sb.Append(chars[b % chars.Length]);

        return sb.ToString();
    }
}