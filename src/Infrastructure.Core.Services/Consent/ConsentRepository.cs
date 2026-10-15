using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Consent;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Consent;
using Infrastructure.Core.Models.Consent;

namespace Infrastructure.Core.Services.Consent;

public sealed class ConsentRepository : BaseDatabaseService, IConsentRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public ConsentRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<ConsentRow?, ConsentRepositoryError>> InsertAsync(
        Guid patientCode, string consentType, string description, DateTimeOffset grantedAt, Guid recordedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, ConsentRow>(
                _connection, _userContext, ConsentRepositorySql.Insert,
                new
                {
                    PatientCode = patientCode,
                    ConsentType = consentType,
                    Description = description,
                    GrantedAt = grantedAt,
                    RecordedByUserCode = recordedByUserCode
                }),
            errorFactory: ConsentRepositoryError (ex) => new InsertConsentError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<ConsentRow?, ConsentRepositoryError>> RevokeAsync(
        Guid patientCode, Guid consentCode, string reason, Guid? revokedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, ConsentRow>(
                _connection, _userContext, ConsentRepositorySql.Revoke,
                new
                {
                    PatientCode = patientCode,
                    ConsentCode = consentCode,
                    Reason = reason,
                    RevokedByUserCode = revokedByUserCode
                }),
            errorFactory: ConsentRepositoryError (ex) => new RevokeConsentError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ConsentRow>, ConsentRepositoryError>> GetPageAsync(
        Guid patientCode, int offset, int limit) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, ConsentRow>(
                _connection, ConsentRepositorySql.GetPage,
                new { PatientCode = patientCode, Offset = offset, Limit = limit }),
            errorFactory: ConsentRepositoryError (ex) => new GetConsentError(ex.Message, ex)
        );
}
