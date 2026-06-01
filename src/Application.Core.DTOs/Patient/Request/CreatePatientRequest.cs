namespace Application.Core.DTOs.Patient.Request;

public sealed record CreatePatientRequest
{
    public required Guid PersonCode { get; init; }

    public IReadOnlyList<PatientAllergyItem> Allergies { get; init; } = [];
}
