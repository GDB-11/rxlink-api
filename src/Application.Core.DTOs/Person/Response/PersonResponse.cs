namespace Application.Core.DTOs.Person.Response;

public sealed class PersonResponse
{
    public required Guid PersonCode { get; init; }
    public required string Names { get; init; }
    public required string Surnames { get; init; }
    public required DateOnly BirthDate { get; init; }
    public required Guid SexCode { get; init; }
    public required string SexName { get; init; }
    public required string Phone { get; init; }
    public string? AlternativePhone { get; init; }
    public required string Email { get; init; }
    public string? Address { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public Guid? DocumentTypeCode { get; init; }
    public string? DocumentTypeName { get; init; }
    public string? DocumentNumber { get; init; }
}
