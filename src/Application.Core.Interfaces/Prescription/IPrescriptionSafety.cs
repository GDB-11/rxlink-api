using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Prescription.Response;
using BindSharp;

namespace Application.Core.Interfaces.Prescription;

/// <summary>Allergy and drug-interaction checks for a prescription (rules come from the shared catalog).</summary>
public interface IPrescriptionSafety
{
    /// <summary>
    /// Checks the prescription's medications against the patient's recorded allergies and against each other and the
    /// patient's other active prescriptions. Fails (rather than returning "no warnings") when the catalog is unavailable.
    /// </summary>
    Task<Result<PrescriptionSafetyCheckResponse, PrescriptionError>> CheckAsync(Guid prescriptionCode);
}
