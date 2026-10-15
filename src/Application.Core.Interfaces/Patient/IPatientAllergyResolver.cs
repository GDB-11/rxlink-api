using Application.Core.DTOs.Patient.Response;
using Infrastructure.Core.Models.Patient;

namespace Application.Core.Interfaces.Patient;

/// <summary>
/// Turns stored patient-allergy rows (codes only) into responses with names from the catalog snapshot.
/// </summary>
public interface IPatientAllergyResolver
{
    PatientAllergyResponse Resolve(PatientAllergyRow row);

    /// <summary>Parses the repository's <c>AllergiesJson</c> and returns the allergies ordered by name.</summary>
    List<PatientAllergyResponse> ResolveJson(string allergiesJson);
}
