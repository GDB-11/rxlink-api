using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.CommunicationPreference;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.CommunicationPreference;
using Infrastructure.Core.Models.CommunicationPreference;

namespace Infrastructure.Core.Services.CommunicationPreference;

public sealed class CommunicationPreferenceRepository : BaseDatabaseService, ICommunicationPreferenceRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public CommunicationPreferenceRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>> GetAsync(
        Guid patientCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, CommunicationPreferenceRow>(
                _connection, CommunicationPreferenceRepositorySql.Get, new { PatientCode = patientCode }),
            errorFactory: CommunicationPreferenceRepositoryError (ex) => new GetCommunicationPreferenceError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>> UpsertAsync(
        Guid patientCode, bool allowEmail, bool allowSms, string? alternativeEmail, string? alternativePhone) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, CommunicationPreferenceRow>(
                _connection, _userContext, CommunicationPreferenceRepositorySql.Upsert,
                new
                {
                    PatientCode = patientCode,
                    AllowEmail = allowEmail,
                    AllowSms = allowSms,
                    AlternativeEmail = alternativeEmail,
                    AlternativePhone = alternativePhone
                }),
            errorFactory: CommunicationPreferenceRepositoryError (ex) => new SaveCommunicationPreferenceError(ex.Message, ex)
        );
}
