using Application.Core.DTOs.Mfa.Errors;
using Application.Core.DTOs.Mfa.Request;
using Application.Core.DTOs.Mfa.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Mfa;
using Application.Core.Interfaces.Shared;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Mfa;
using Infrastructure.Core.Models.Mfa;

namespace Application.Core.Services.Mfa;

public sealed class MfaService : IMfa
{
    internal const string Issuer = "RxLink";

    private readonly IMfaRepository _repository;
    private readonly ITotp _totp;
    private readonly IChaChaEncryption _encryption;
    private readonly IActivityLogger _activityLogger;
    private readonly IUserContext _userContext;

    public MfaService(
        IMfaRepository repository,
        ITotp totp,
        IChaChaEncryption encryption,
        IActivityLogger activityLogger,
        IUserContext userContext)
    {
        _repository = repository;
        _totp = totp;
        _encryption = encryption;
        _activityLogger = activityLogger;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task<Result<MfaStatusResponse, MfaError>> GetStatusAsync(Guid userCode) =>
        GetRowAsync(userCode)
            .MapAsync(row => new MfaStatusResponse
            {
                Enabled = row is { IsConfirmed: true },
                Pending = row is { IsConfirmed: false }
            });

    /// <inheritdoc/>
    public Task<Result<MfaEnrollmentResponse, MfaError>> EnrollAsync(Guid userCode, string accountName)
    {
        string secret = _totp.GenerateSecret();

        return GetRowAsync(userCode)
            .EnsureAsync(row => row is not { IsConfirmed: true }, new MfaAlreadyEnabledError())
            .BindAsync(_ => Task.FromResult(_encryption.Encrypt(secret)
                .MapError(MfaError (e) => new MfaSecretProtectionError(e.Message, e.Details, e.Exception))))
            .BindAsync(encrypted => _repository.UpsertPendingAsync(userCode, encrypted)
                .MapErrorAsync(MfaError (e) => new MfaDataAccessError(e.Message, e.Details, e.Exception)))
            .EnsureAsync(affected => affected > 0, new MfaAlreadyEnabledError())
            .MapAsync(_ => new MfaEnrollmentResponse
            {
                Secret = secret,
                OtpauthUri = _totp.BuildOtpauthUri(secret, Issuer, accountName)
            });
    }

    /// <inheritdoc/>
    public Task<Result<Unit, MfaError>> ConfirmAsync(Guid userCode, ConfirmMfaRequest request) =>
        GetRowAsync(userCode)
            .EnsureAsync(row => row is { IsConfirmed: false }, new MfaNotPendingError())
            .BindAsync(row => ValidateCode(row!, request.Code))
            .BindAsync(step => _repository.ConfirmAsync(userCode, step)
                .MapErrorAsync(MfaError (e) => new MfaDataAccessError(e.Message, e.Details, e.Exception)))
            .EnsureAsync(affected => affected > 0, new MfaNotPendingError())
            .TapAsync(_ => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.MfaEnrolled, userCode, ActivityEntityTypes.User, userCode, "MFA enrolled")))
            .TapErrorAsync(error => error is MfaInvalidCodeError ? LogFailureAsync(userCode, "Confirm") : Task.CompletedTask)
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, MfaError>> ResetAsync(Guid targetUserCode) =>
        _repository.DeleteAsync(targetUserCode)
            .MapErrorAsync(MfaError (e) => new MfaDataAccessError(e.Message, e.Details, e.Exception))
            .EnsureAsync(affected => affected > 0, new MfaNotEnabledError())
            .TapAsync(_ => _activityLogger.LogAsync(new ActivityEvent(
                ActivityEventTypes.MfaReset, _userContext.UserCode, ActivityEntityTypes.User, targetUserCode,
                "MFA reset by administrator")))
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, MfaError>> VerifyCodeAsync(Guid userCode, string code) =>
        GetRowAsync(userCode)
            .EnsureAsync(row => row is { IsConfirmed: true }, new MfaNotEnabledError())
            .BindAsync(row => ValidateCode(row!, code))
            // The conditional UPDATE makes the "step not used yet" check atomic, so a replayed code (or a
            // concurrent duplicate) affects 0 rows and is rejected.
            .BindAsync(step => _repository.AdvanceStepAsync(userCode, step)
                .MapErrorAsync(MfaError (e) => new MfaDataAccessError(e.Message, e.Details, e.Exception)))
            .EnsureAsync(affected => affected > 0, new MfaInvalidCodeError())
            .TapErrorAsync(error => error is MfaInvalidCodeError ? LogFailureAsync(userCode, "Login") : Task.CompletedTask)
            .MapAsync(_ => Unit.Value);

    private Task<Result<MfaRow?, MfaError>> GetRowAsync(Guid userCode) =>
        _repository.GetByUserCodeAsync(userCode)
            .MapErrorAsync(MfaError (e) => new MfaDataAccessError(e.Message, e.Details, e.Exception));

    /// <summary>Decrypts the stored secret and returns the matching TOTP step, rejecting steps at or before the last used one.</summary>
    private Task<Result<long, MfaError>> ValidateCode(MfaRow row, string code)
    {
        Result<long, MfaError> result = _encryption.Decrypt(row.SecretEncrypted)
            .MapError(MfaError (e) => new MfaSecretProtectionError(e.Message, e.Details, e.Exception))
            .Bind(secret => _totp.Validate(secret, code) is { } step && step > (row.LastUsedStep ?? long.MinValue)
                ? Result<long, MfaError>.Success(step)
                : Result<long, MfaError>.Failure(new MfaInvalidCodeError()));

        return Task.FromResult(result);
    }

    private Task LogFailureAsync(Guid userCode, string stage) =>
        _activityLogger.LogAsync(new ActivityEvent(
            ActivityEventTypes.MfaFailed, userCode, ActivityEntityTypes.User, userCode, "MFA code rejected",
            new { Stage = stage }));
}
