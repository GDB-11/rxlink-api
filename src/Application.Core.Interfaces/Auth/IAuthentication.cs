using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Auth.Request;
using Application.Core.DTOs.Auth.Response;
using BindSharp;

namespace Application.Core.Interfaces.Auth;

public interface IAuthentication
{
    Task<Result<LoginResponse, AuthenticationError>> LoginAsync(LoginRequest request);
    /// <summary>Second login step for MFA-enabled accounts: exchanges the challenge token + code for session tokens.</summary>
    Task<Result<LoginResponse, AuthenticationError>> VerifyMfaAsync(MfaVerifyRequest request);
    Task<Result<LoginResponse, AuthenticationError>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<Result<Unit, AuthenticationError>> LogoutAsync(LogoutRequest request);
}