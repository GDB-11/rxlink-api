namespace Application.Core.DTOs.PartnerConfig.Response;

public sealed class PartnerLogoResponse
{
    public required string ContentType { get; init; }
    public required byte[] Data { get; init; }
    public required string Sha256 { get; init; }
}
