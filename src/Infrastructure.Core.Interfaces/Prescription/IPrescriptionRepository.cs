using BindSharp;
using Infrastructure.Core.DTOs.Prescription;
using Infrastructure.Core.Models.Prescription;

namespace Infrastructure.Core.Interfaces.Prescription;

public interface IPrescriptionRepository
{
    /// <summary>
    /// Inserts a new prescription (status: Borrador) with its detail lines.
    /// Returns null when the diagnostic is not found or inactive.
    /// Returns <see cref="InsertPrescriptionDuplicateError"/> when a non-deleted prescription already exists for the diagnostic.
    /// </summary>
    Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> InsertAsync(
        Guid diagnosticCode, string? notes, DateOnly validUntil, string detailsJson, Guid createdByUserCode);

    /// <summary>
    /// Returns the full prescription with detail lines.
    /// Returns null when not found or deleted.
    /// </summary>
    Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> GetByCodeAsync(Guid code);

    /// <summary>
    /// Updates notes, validUntil and lines. Only succeeds when status is Borrador.
    /// Returns null when not found or deleted.
    /// Returns <see cref="UpdatePrescriptionInvalidStatusError"/> when status is not Borrador.
    /// </summary>
    Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> UpdateAsync(
        Guid code, string? notes, DateOnly validUntil, string detailsJson, Guid modifiedByUserCode);

    /// <summary>Transitions Borrador → Activo. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> SignAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Activo → Suspendido. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> SuspendAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Suspendido → Activo. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> ReactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions any non-terminal status → Cancelado. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> CancelAsync(Guid code, Guid performedByUserCode);

    /// <summary>Transitions Activo → Dispensado. Returns 0 when the transition is invalid.</summary>
    Task<Result<int, PrescriptionRepositoryError>> DispenseAsync(Guid code, Guid performedByUserCode);
}