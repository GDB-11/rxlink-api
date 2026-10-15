namespace Infrastructure.Core.DTOs.Appointment;

/// <summary>
/// The insurer terms a payment snapshot is written with: the effective coverage (partner override or catalog default)
/// and the insurer's name at payment time. Resolved by the service layer from the catalog snapshot.
/// </summary>
public sealed record InsuranceTerms(Guid InsuranceCode, string InsuranceName, decimal CoveragePercentage);
