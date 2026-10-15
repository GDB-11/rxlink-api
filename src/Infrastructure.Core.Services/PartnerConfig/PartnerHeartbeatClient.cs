using System.Net.Http.Headers;
using System.Net.Http.Json;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerConfig;
using Infrastructure.Core.Interfaces.PartnerConfig;
using Infrastructure.Core.Models.PartnerConfig;

namespace Infrastructure.Core.Services.PartnerConfig;

public sealed class PartnerHeartbeatClient : IPartnerHeartbeatClient
{
    private const string Path = "api/v1/partners/me/heartbeat";

    private readonly HttpClient _http;
    private readonly string _apiKey;

    /// <param name="http">Client whose <c>BaseAddress</c> is the catalog API.</param>
    public PartnerHeartbeatClient(HttpClient http, string apiKey)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public async Task<Result<Unit, PartnerConfigRepositoryError>> SendAsync(
        HeartbeatPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, Path)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken);

            // Status code only: the response body is not ours to log.
            return response.IsSuccessStatusCode
                ? Result<Unit, PartnerConfigRepositoryError>.Success(Unit.Value)
                : Result<Unit, PartnerConfigRepositoryError>.Failure(new HeartbeatError($"HTTP {(int)response.StatusCode}"));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return Result<Unit, PartnerConfigRepositoryError>.Failure(new HeartbeatError(ex.GetType().Name, ex));
        }
    }
}
