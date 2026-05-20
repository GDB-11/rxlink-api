using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Core.Config;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using BindSharp;
using Infrastructure.Core.Models.Account;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Application.Core.Services.Auth;

public sealed class JwtService : IJwt
{
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

    private (string AccessToken, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        DateTime issuedAt = _timeProvider.UtcNow;
        DateTime expiresAt = issuedAt.AddMinutes(_jwtConfig.AccessTokenExpiryMinutes);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub,   user.UserCode.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name,  $"{user.Names} {user.Surnames}"),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.RoleName),
            .. user.LicenseNumber is not null
                ? (Claim[])[new Claim("license_number", user.LicenseNumber)]
                : []
        ];

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
    public string GenerateRefreshToken()
    {
        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}