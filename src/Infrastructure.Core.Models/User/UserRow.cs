namespace Infrastructure.Core.Models.Users;

/// <summary>
/// Flat row returned by user queries.
/// Combines columns from User, Person, PersonDocument, Role, Specialty, Sex, and DocumentType.
/// Catalog integer IDs are not projected; only public UUIDs (codes) are exposed.
/// <c>TotalCount</c> is populated by the paginated query via a window function;
/// single-row operations (insert/update) return 0.
/// </summary>
public sealed class UserRow
{
    public required Guid UserCode { get; init; }
    public required Guid PersonCode { get; init; }

    // Person
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

    // PersonDocument (optional — a person may have no document yet)
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

    // Pagination
    public required long TotalCount { get; init; }
}
