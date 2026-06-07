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
    Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError> GeneratePatientAccessToken(PatientCredential patient);

    /// <summary>
    /// Generates a cryptographically secure, opaque refresh token.
    /// The returned value is the raw token to be sent to the client.
    /// Storing it is the caller's responsibility — pass it through
    /// <see cref="IDeterministicEncryption"/> before persisting.
    /// </summary>
    string GenerateRefreshToken();
}