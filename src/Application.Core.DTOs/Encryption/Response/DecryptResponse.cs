namespace Application.Core.DTOs.Encryption.Response;

public sealed record DecryptResponse
{
    public required string PlainText { get; init; }
}