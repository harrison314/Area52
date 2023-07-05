using System.Security.Cryptography;

namespace Area52.Services.Implementation.Utils;

internal static class ApiKeyBuilder
{
    private static string Aphabet = "QWERTZUIOPASDFGHJKLYXCVBNM0123456789qwertzuiopasdfghjklyxcvbnm_";

    public static string BuildApiKey(int length = 32)
    {
        return string.Create<int>(length, 0, (strSpan, _) =>
        {
            Span<byte> data = (strSpan.Length < 1024) ? stackalloc byte[strSpan.Length] : new byte[strSpan.Length];
            RandomNumberGenerator.Fill(data);

            for (int i = 0; i < strSpan.Length; i++)
            {
                strSpan[i] = Aphabet[data[i] % Aphabet.Length];
            }
        });
    }
}
