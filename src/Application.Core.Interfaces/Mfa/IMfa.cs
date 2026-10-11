using Application.Core.DTOs.Mfa.Errors;
using Application.Core.DTOs.Mfa.Request;
using Application.Core.DTOs.Mfa.Response;
using BindSharp;

namespace Application.Core.Interfaces.Mfa;

public interface IMfa
{
    Task<Result<MfaStatusResponse, MfaError>> GetStatusAsync(Guid userCode);

    /// <summary>Starts (or restarts) enrollment and returns the secret to load into an authenticator app.</summary>
    Task<Result<MfaEnrollmentResponse, MfaError>> EnrollAsync(Guid userCode, string accountName);

    /// <summary>Confirms a pending enrollment with a code from the authenticator app; MFA is enforced from then on.</summary>
    Task<Result<Unit, MfaError>> ConfirmAsync(Guid userCode, ConfirmMfaRequest request);

    /// <summary>Administrator reset of a user's MFA (lost device).</summary>
    Task<Result<Unit, MfaError>> ResetAsync(Guid targetUserCode);

    /// <summary>Verifies a login code for a user with confirmed MFA, rejecting replays of an already used step.</summary>
    Task<Result<Unit, MfaError>> VerifyCodeAsync(Guid userCode, string code);
}
