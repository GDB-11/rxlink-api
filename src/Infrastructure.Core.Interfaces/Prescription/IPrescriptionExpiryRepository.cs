namespace Infrastructure.Core.Interfaces.Prescription;

/// <summary>A prescription moved to a terminal status by the expiry sweep.</summary>
public sealed record ExpiredPrescriptionRow(Guid PrescriptionCode, string NewStatus);

public interface IPrescriptionExpiryRepository
{
    /// <summary>
    /// Moves up to <paramref name="batchSize"/> prescriptions whose <c>ValidUntil</c> is before today (America/Lima)
    /// to their terminal status — <c>Activo</c>/<c>Suspendido</c> → <c>Caducado</c>, <c>Dispensado</c> → <c>Finalizado</c> —
    /// and records one <c>ActivityEvent</c> per prescription in the same transaction. Returns what changed.
    /// </summary>
    Task<IReadOnlyList<ExpiredPrescriptionRow>> ExpireBatchAsync(int batchSize);
}
