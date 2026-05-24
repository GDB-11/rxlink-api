namespace Application.Core.DTOs.User.Request;

public sealed record UpdateUserRequest
{
    // Person
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required Guid SexCode { get; init; }
    public required string Phone { get; init; }
    public string? AlternativePhone { get; init; }
    public required string PersonEmail { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }

    // PersonDocument
    public required Guid DocumentTypeCode { get; init; }
    public required string DocumentNumber { get; init; }
    public DateOnly? DocumentIssueDate { get; init; }
    public DateOnly? DocumentExpirationDate { get; init; }

    // User account (password is not changed through this endpoint)
    public required string RoleName { get; init; }
    public Guid? SpecialtyCode { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? LicenseNumber { get; init; }
}
