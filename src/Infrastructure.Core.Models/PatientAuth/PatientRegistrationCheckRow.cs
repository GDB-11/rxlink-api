namespace Infrastructure.Core.Models.PatientAuth;

public sealed class PatientRegistrationCheckRow
{
    public required Guid PersonCode { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required string Email { get; init; }
    public Guid? PatientCode { get; init; }
    public bool HasCredentials { get; init; }
}
