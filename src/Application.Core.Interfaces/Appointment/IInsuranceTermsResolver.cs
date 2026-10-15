using Application.Core.DTOs.Catalog.Errors;
using BindSharp;
using Infrastructure.Core.DTOs.Appointment;

namespace Application.Core.Interfaces.Appointment;

/// <summary>
/// Validates the insurer chosen for a payment (active in the catalog, enabled by the partner) and resolves the terms
/// the payment snapshot is written with: the insurer's name and its effective coverage (the partner's override, else
/// the catalog default).
/// </summary>
public interface IInsuranceTermsResolver
{
    /// <summary>Yields <c>null</c> terms when <paramref name="insuranceCode"/> is <c>null</c> (payment without insurance).</summary>
    Task<Result<InsuranceTerms?, CatalogValidationError>> ResolveAsync(Guid? insuranceCode);
}
