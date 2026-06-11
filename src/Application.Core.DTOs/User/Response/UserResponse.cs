namespace Application.Core.DTOs.User.Response;

public sealed class UserResponse
{
    public required Guid UserCode { get; init; }
    public required Guid PersonCode { get; init; }

    // Person (read-only — managed via /api/person)
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required Guid SexCode { get; init; }
    public required string SexName { get; init; }
    public required string Phone { get; init; }
    public string? AlternativePhone { get; init; }
    public required string PersonEmail { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }

    // PersonDocument
    public Guid? DocumentTypeCode { get; init; }
    public string? DocumentTypeName { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? DocumentIssueDate { get; init; }
    public DateOnly? DocumentExpirationDate { get; init; }

    // Role
    public required Guid RoleCode { get; init; }
    public required string RoleName { get; init; }

    // Specialty (optional)
    public Guid? SpecialtyCode { get; init; }
    public string? SpecialtyName { get; init; }

    // User account
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? LicenseNumber { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
}