using System.Net;
using System.Text.Json;
using Infrastructure.Core.DTOs.PartnerConfig;
using Infrastructure.Core.Models.PartnerConfig;
using Infrastructure.Core.Services.PartnerConfig;

namespace Application.Core.Services.Tests.PartnerConfig;

public sealed class PartnerHeartbeatClientTests
{
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }

    private static readonly HeartbeatPayload Payload = new()
    {
        ApiVersion = "1.2.3",
        SupportedCatalogSchemas = new SchemaRange(1, 2),
        CatalogSchemaVersionSeen = 1,
        CatalogVersionSeen = 42
    };

    private static PartnerHeartbeatClient Client(FakeHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://catalog.example/") }, "secret-key");

    [Fact]
    public async Task Send_PostsPayloadWithApiKey()
    {
        FakeHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await Client(handler).SendAsync(Payload, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://catalog.example/api/v1/partners/me/heartbeat", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("secret-key", handler.Request.Headers.Authorization.Parameter);

        using JsonDocument body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("1.2.3", body.RootElement.GetProperty("apiVersion").GetString());
        Assert.Equal(42, body.RootElement.GetProperty("catalogVersionSeen").GetInt64());
        Assert.Equal(2, body.RootElement.GetProperty("supportedCatalogSchemas").GetProperty("max").GetInt32());
    }

    [Fact]
    public async Task Send_ErrorStatus_FailsWithStatusOnly()
    {
        FakeHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("secret body") });

        var result = await Client(handler).SendAsync(Payload, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.IsType<HeartbeatError>(result.Error);
        Assert.Equal("HTTP 401", result.Error!.Details);
    }

    [Fact]
    public async Task Send_NetworkFailure_FailsWithoutThrowing()
    {
        FakeHandler handler = new(_ => throw new HttpRequestException("connection refused"));

        var result = await Client(handler).SendAsync(Payload, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(nameof(HttpRequestException), result.Error!.Details);
    }
}
