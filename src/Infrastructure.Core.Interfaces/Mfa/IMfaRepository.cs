using BindSharp;
using Infrastructure.Core.DTOs.Mfa;
using Infrastructure.Core.Models.Mfa;

namespace Infrastructure.Core.Interfaces.Mfa;

public interface IMfaRepository
{
    /// <summary>Returns the user's MFA row (pending or confirmed), or <c>null</c> if none.</summary>
    Task<Result<MfaRow?, MfaRepositoryError>> GetByUserCodeAsync(Guid userCode);

    /// <summary>
    /// Creates or replaces a pending enrollment. Returns affected rows: <c>0</c> when the user already has a
    /// confirmed MFA (it must be reset first).
    /// </summary>
    Task<Result<int, MfaRepositoryError>> UpsertPendingAsync(Guid userCode, string secretEncrypted);

    /// <summary>Confirms a pending enrollment, recording the step of the confirming code. Returns affected rows.</summary>
    Task<Result<int, MfaRepositoryError>> ConfirmAsync(Guid userCode, long step);

    /// <summary>
    /// Atomically records <paramref name="step"/> as used. Returns <c>0</c> when that step (or a later one) was
    /// already used, i.e. the code is a replay.
    /// </summary>
    Task<Result<int, MfaRepositoryError>> AdvanceStepAsync(Guid userCode, long step);

    /// <summary>Removes the user's MFA (admin reset). Returns affected rows.</summary>
    Task<Result<int, MfaRepositoryError>> DeleteAsync(Guid userCode);
}
