using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.PrivacyNotice;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PrivacyNotice;
using Infrastructure.Core.Models.PrivacyNotice;

namespace Infrastructure.Core.Services.PrivacyNotice;

public sealed class PrivacyNoticeRepository : BaseDatabaseService, IPrivacyNoticeRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public PrivacyNoticeRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>> PublishAsync(
        string title, string body, DateOnly effectiveFrom, Guid publishedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, PrivacyNoticeRow>(
                _connection, _userContext, PrivacyNoticeRepositorySql.Publish,
                new { Title = title, Body = body, // Dapper cannot bind DateOnly; the column is a DATE.
                EffectiveFrom = effectiveFrom.ToDateTime(TimeOnly.MinValue), PublishedByUserCode = publishedByUserCode }),
            errorFactory: PrivacyNoticeRepositoryError (ex) => new PublishPrivacyNoticeError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PrivacyNoticeRow>, PrivacyNoticeRepositoryError>> GetPageAsync(
        int offset, int limit) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, PrivacyNoticeRow>(
                _connection, PrivacyNoticeRepositorySql.GetPage, new { Offset = offset, Limit = limit }),
            errorFactory: PrivacyNoticeRepositoryError (ex) => new GetPrivacyNoticeError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>> GetCurrentAsync() =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PrivacyNoticeRow>(
                _connection, PrivacyNoticeRepositorySql.GetCurrent, new { }),
            errorFactory: PrivacyNoticeRepositoryError (ex) => new GetPrivacyNoticeError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError>>
        GetAcknowledgmentsAsync(Guid patientCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, PrivacyNoticeAcknowledgmentRow>(
                _connection, PrivacyNoticeRepositorySql.GetAcknowledgments, new { PatientCode = patientCode }),
            errorFactory: PrivacyNoticeRepositoryError (ex) => new GetPrivacyNoticeError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PrivacyNoticeRepositoryError>> AcknowledgeCurrentAsync(
        Guid patientCode, Guid? recordedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(
                _connection, _userContext, PrivacyNoticeRepositorySql.AcknowledgeCurrent,
                new { PatientCode = patientCode, RecordedByUserCode = recordedByUserCode }),
            errorFactory: PrivacyNoticeRepositoryError (ex) => new AcknowledgePrivacyNoticeError(ex.Message, ex)
        );
}
