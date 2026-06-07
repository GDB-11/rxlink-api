namespace Application.Core.DTOs.PatientAuth.Response;

public sealed record PatientAuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required PatientInfo Patient { get; init; }
}
