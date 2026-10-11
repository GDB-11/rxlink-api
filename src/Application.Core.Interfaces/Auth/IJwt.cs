using Application.Core.DTOs.Auth.Errors;
using Application.Core.Interfaces.Shared;
using BindSharp;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.PatientAuth;

namespace Application.Core.Interfaces.Auth;

public interface IJwt
{
    /// <summary>Generates a signed JWT access token for the given user.</summary>
    Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError> GenerateAccessToken(User user);

    /// <summary>Generates a signed JWT access token for a patient (Android app).</summary>
    Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError> GeneratePatientAccessToken(
        PatientCredential patient);

    /// <summary>
    /// Generates the short-lived token handed out after a correct password for an MFA-enabled user. It carries a
    /// different audience from access tokens, so it is rejected by the API's bearer authentication.
    /// </summary>
    Result<(string Token, DateTime ExpiresAt), AuthenticationError> GenerateMfaToken(User user);

    /// <summary>Validates an MFA challenge token and returns the <c>UserCode</c> it was issued for.</summary>
    Task<Result<Guid, AuthenticationError>> ValidateMfaTokenAsync(string token);

    /// <summary>
    /// Generates a cryptographically secure, opaque refresh token.
    /// The returned value is the raw token to be sent to the client.
    /// Storing it is the caller's responsibility — pass it through
    /// <see cref="IDeterministicEncryption"/> before persisting.
    /// </summary>
    string GenerateRefreshToken();
}