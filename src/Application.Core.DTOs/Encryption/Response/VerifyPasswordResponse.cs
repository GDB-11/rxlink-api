namespace Application.Core.DTOs.Encryption.Response;

public sealed record VerifyPasswordResponse
{
    public required bool IsMatch { get; init; }
}
