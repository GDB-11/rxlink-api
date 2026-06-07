namespace Application.Core.DTOs.PatientAuth.Response;

public sealed record PatientLookupResponse
{
    public required bool Found { get; init; }
    public Guid? PersonCode { get; init; }
    public string? Names { get; init; }
    public string? Surnames { get; init; }
    public string? Email { get; init; }
    public bool IsAlreadyPatient { get; init; }
}
