using System.Security.Cryptography;
using System.Text;
using Application.Core.Interfaces.Auth;

namespace Application.Core.Services.Auth;

/// <summary>
/// Have I Been Pwned "range" lookup. Only the first 5 hex characters of the SHA-1 hash leave the
/// server (k-anonymity); the password and the full hash are never transmitted.
/// </summary>
public sealed class PwnedPasswordsChecker : IBreachedPasswordChecker
{
    private readonly HttpClient _http;

    public PwnedPasswordsChecker(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken = default)
    {
        try
        {
            string hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
            string prefix = hash[..5];
            string suffix = hash[5..];

            using HttpRequestMessage request = new(HttpMethod.Get, $"range/{prefix}");
            request.Headers.Add("Add-Padding", "true");

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return false;

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            return ContainsSuffix(body, suffix);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false; // fail open: a third-party outage must not block password changes
        }
    }

    /// <summary>Response lines are <c>SUFFIX:COUNT</c>; padded decoy entries have a count of 0.</summary>
    internal static bool ContainsSuffix(string body, string suffix)
    {
        foreach (string line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Trim().Split(':');
            if (parts.Length == 2
                && parts[0].Equals(suffix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], out int count) && count > 0)
                return true;
        }

        return false;
    }
}
