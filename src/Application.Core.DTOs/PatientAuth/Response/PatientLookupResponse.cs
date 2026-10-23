namespace Application.Core.DTOs.PatientAuth.Response;

/// <summary>
/// Deliberately minimal: no names or e-mail, and an unknown document looks exactly like a wrong birth date
/// (<c>Found = false</c>), so the endpoint cannot be used to enumerate patients.
/// </summary>
public sealed record PatientLookupResponse
{
    public required bool Found { get; init; }
    public Guid? PersonCode { get; init; }

    /// <summary>The person already has an app account: the client should offer login instead of registration.</summary>
    public bool HasAccount { get; init; }
}
