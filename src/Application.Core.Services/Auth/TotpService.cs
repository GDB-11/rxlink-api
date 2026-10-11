using System.Security.Cryptography;
using System.Text;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;

namespace Application.Core.Services.Auth;

public sealed class TotpService : ITotp
{
    private const int StepSeconds = 30;
    private const int Digits = 6;
    private const int DriftSteps = 1;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private readonly ITimeProvider _timeProvider;

    public TotpService(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(20));

    /// <inheritdoc/>
    public string BuildOtpauthUri(string secret, string issuer, string accountName) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}" +
        $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";

    /// <inheritdoc/>
    public long? Validate(string secret, string code)
    {
        if (code.Length != Digits || !code.All(char.IsAsciiDigit))
            return null;

        byte[]? key = Base32Decode(secret);
        if (key is null)
            return null;

        long current = new DateTimeOffset(_timeProvider.UtcNow, TimeSpan.Zero).ToUnixTimeSeconds() / StepSeconds;
        byte[] supplied = Encoding.ASCII.GetBytes(code);
        long? match = null;

        // No early exit: every candidate step is compared in constant time.
        for (long step = current - DriftSteps; step <= current + DriftSteps; step++)
        {
            byte[] expected = Encoding.ASCII.GetBytes(ComputeCode(key, step));
            if (CryptographicOperations.FixedTimeEquals(expected, supplied))
                match = step;
        }

        return match;
    }

    internal static string ComputeCode(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, step);

        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(key, counter, hash);

        int offset = hash[^1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];

        return (binary % 1_000_000).ToString("D6");
    }

    private static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        return sb.ToString();
    }

    private static byte[]? Base32Decode(string value)
    {
        var bytes = new List<byte>(value.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (char c in value.TrimEnd('=').ToUpperInvariant())
        {
            int index = Base32Alphabet.IndexOf(c);
            if (index < 0)
                return null;

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return bytes.ToArray();
    }
}
