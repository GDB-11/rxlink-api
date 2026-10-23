using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Core.Config;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using BindSharp;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.PatientAuth;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Application.Core.Services.Auth;

public sealed class JwtService : IJwt
{
    private const string MfaAudience = "rxlink-mfa-challenge";
    private const int MfaTokenMinutes = 5;

    private readonly JwtConfig _jwtConfig;
    private readonly ITimeProvider _timeProvider;

    public JwtService(JwtConfig jwtConfig, ITimeProvider timeProvider)
    {
        _jwtConfig = jwtConfig;
        _timeProvider = timeProvider;
    }

    public Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError> GenerateAccessToken(User user) =>
        Result.Try(
            operation: () => CreateAccessToken(user),
            errorFactory: AuthenticationError (ex) => new JwtGenerationError(ex.Message, ex)
        );

    public Result<(string AccessToken, DateTime ExpiresAt), AuthenticationError> GeneratePatientAccessToken(
        PatientCredential patient) =>
        Result.Try(
            operation: () => CreatePatientAccessToken(patient),
            errorFactory: AuthenticationError (ex) => new JwtGenerationError(ex.Message, ex)
        );

    private (string AccessToken, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        DateTime issuedAt = _timeProvider.UtcNow;
        DateTime expiresAt = issuedAt.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.UserCode.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, $"{user.Names} {user.Surnames}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.RoleName),
            .. user.LicenseNumber is not null
                ? (Claim[])[new Claim("license_number", user.LicenseNumber)]
                : []
        ];

        return BuildToken(claims, issuedAt, _jwtConfig.AccessTokenExpiryMinutes);
    }

    private (string AccessToken, DateTime ExpiresAt) CreatePatientAccessToken(PatientCredential patient)
    {
        DateTime issuedAt = _timeProvider.UtcNow;

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, patient.Email),
            new(JwtRegisteredClaimNames.Email, patient.Email),
            new(JwtRegisteredClaimNames.Name, $"{patient.Names} {patient.Surnames}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "Patient"),
            new("patient_code", patient.PatientCode.ToString())
        ];

        return BuildToken(claims, issuedAt, _jwtConfig.PatientAccessTokenExpiryMinutes);
    }

    private (string AccessToken, DateTime ExpiresAt) BuildToken(Claim[] claims, DateTime issuedAt, int expiryMinutes)
    {
        DateTime expiresAt = issuedAt.AddMinutes(expiryMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            IssuedAt = issuedAt,
            Issuer = _jwtConfig.Issuer,
            Audience = _jwtConfig.Audience,
            SigningCredentials = credentials
        };

        string accessToken = new JsonWebTokenHandler().CreateToken(tokenDescriptor);

        return (accessToken, expiresAt);
    }

    /// <inheritdoc/>
    public Result<(string Token, DateTime ExpiresAt), AuthenticationError> GenerateMfaToken(User user) =>
        Result.Try(
            operation: () =>
            {
                DateTime issuedAt = _timeProvider.UtcNow;
                DateTime expiresAt = issuedAt.AddMinutes(MfaTokenMinutes);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity([
                        new Claim(JwtRegisteredClaimNames.Sub, user.UserCode.ToString()),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    ]),
                    Expires = expiresAt,
                    IssuedAt = issuedAt,
                    Issuer = _jwtConfig.Issuer,
                    Audience = MfaAudience,
                    SigningCredentials = new SigningCredentials(SigningKey(), SecurityAlgorithms.HmacSha256)
                };

                return (new JsonWebTokenHandler().CreateToken(tokenDescriptor), expiresAt);
            },
            errorFactory: AuthenticationError (ex) => new JwtGenerationError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<Guid, AuthenticationError>> ValidateMfaTokenAsync(string token)
    {
        TokenValidationResult validation = await new JsonWebTokenHandler().ValidateTokenAsync(token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = MfaAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SigningKey(),
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            });

        return validation.IsValid
               && validation.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out object? sub)
               && Guid.TryParse(sub.ToString(), out Guid userCode)
            ? Result<Guid, AuthenticationError>.Success(userCode)
            : Result<Guid, AuthenticationError>.Failure(new InvalidMfaTokenError());
    }

    private SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(_jwtConfig.SecretKey));

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}