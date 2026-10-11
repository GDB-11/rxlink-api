using System.Net;
using System.Security.Cryptography;
using System.Text;
using Application.Core.Services.Auth;

namespace Application.Core.Services.Tests.Auth;

public sealed class PwnedPasswordsCheckerTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private static string Sha1(string value) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)));

    private static PwnedPasswordsChecker Create(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

    private static HttpResponseMessage Ok(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    [Fact]
    public async Task IsBreachedAsync_SuffixListedWithCount_ReturnsTrueAndSendsOnlyPrefix()
    {
        string hash = Sha1("password-123");
        var handler = new StubHandler(_ => Ok($"0000000000000000000000000000000000A:3\r\n{hash[5..]}:42\r\n"));

        bool breached = await Create(handler).IsBreachedAsync("password-123");

        Assert.True(breached);
        Assert.Equal($"/range/{hash[..5]}", handler.LastUri!.AbsolutePath);
        Assert.DoesNotContain(hash[5..], handler.LastUri.ToString());
    }

    [Fact]
    public async Task IsBreachedAsync_PaddingEntryWithZeroCount_ReturnsFalse()
    {
        string hash = Sha1("unique-passphrase-xyz");
        var handler = new StubHandler(_ => Ok($"{hash[5..]}:0\r\n"));

        Assert.False(await Create(handler).IsBreachedAsync("unique-passphrase-xyz"));
    }

    [Fact]
    public async Task IsBreachedAsync_ServiceError_FailsOpen()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        Assert.False(await Create(handler).IsBreachedAsync("anything-at-all"));
    }

    [Fact]
    public async Task IsBreachedAsync_NetworkFailure_FailsOpen()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("down"));

        Assert.False(await Create(handler).IsBreachedAsync("anything-at-all"));
    }
}
