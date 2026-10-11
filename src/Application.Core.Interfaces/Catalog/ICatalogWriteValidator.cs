using Application.Core.DTOs.Catalog.Errors;
using BindSharp;

namespace Application.Core.Interfaces.Catalog;

/// <summary>
/// Checks catalog codes on writes against the shared-catalog snapshot (and the partner's enabled flags), replacing
/// the foreign keys the database used to enforce. Deactivated items are rejected for new use.
/// </summary>
public interface ICatalogWriteValidator
{
    /// <summary>Medication (active and enabled by the partner), route and frequency (active) of prescription lines.</summary>
    Task<Result<Unit, CatalogValidationError>> ValidatePrescriptionLinesAsync(
        IReadOnlyCollection<(Guid MedicationCode, Guid AdministrationRouteCode, Guid FrequencyCode)> lines);

    /// <summary>Specialty is active and enabled by the partner. <c>null</c> passes (not every user has one).</summary>
    Task<Result<Unit, CatalogValidationError>> ValidateSpecialtyAsync(Guid? specialtyCode);

    /// <summary>Insurance is active and enabled by the partner. <c>null</c> passes (self-pay).</summary>
    Task<Result<Unit, CatalogValidationError>> ValidateInsuranceAsync(Guid? insuranceCode);

    /// <summary>Sex and document type are active.</summary>
    Task<Result<Unit, CatalogValidationError>> ValidatePersonAsync(Guid sexCode, Guid documentTypeCode);

    /// <summary>Allergy and severity pairs are active.</summary>
    Task<Result<Unit, CatalogValidationError>> ValidateAllergiesAsync(
        IReadOnlyCollection<(Guid AllergyCode, Guid SeverityCode)> allergies);
}
