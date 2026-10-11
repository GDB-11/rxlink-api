using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.PatientAccess;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PatientAccess;
using Infrastructure.Core.Models.PatientAccess;

namespace Infrastructure.Core.Services.PatientAccess;

public sealed class PatientAccessRepository : BaseDatabaseService, IPatientAccessRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;
    private readonly IActivityLogger _activityLogger;

    public PatientAccessRepository(IDbConnection connection, IUserContext userContext,
        IActivityLogger activityLogger)
    {
        _connection = connection;
        _userContext = userContext;
        _activityLogger = activityLogger;
    }

    /// <inheritdoc/>
    public async Task<Result<PatientAccessFactsRow, PatientAccessRepositoryError>> GetFactsAsync(
        Guid userCode, Guid patientCode) =>
        await Result.TryAsync(
            operation: async () => (await ExecuteFirstOrDefaultAsync<object, PatientAccessFactsRow>(
                _connection,
                PatientAccessRepositorySql.GetFacts,
                new { UserCode = userCode, PatientCode = patientCode }))!,
            errorFactory: PatientAccessRepositoryError (ex) => new GetPatientAccessFactsError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<BreakGlassGrantRow?, PatientAccessRepositoryError>> GrantBreakGlassAsync(
        Guid userCode, string medicalRecordNumber, string reason, int ttlMinutes) =>
        await Result.TryAsync(
            operation: () => ExecuteAuditedWriteAsync(
                _connection,
                _userContext,
                async transaction =>
                {
                    BreakGlassGrantRow? row = await ExecuteFirstOrDefaultAsync<object, BreakGlassGrantRow>(
                        _connection,
                        PatientAccessRepositorySql.GrantBreakGlass,
                        new
                        {
                            UserCode = userCode,
                            MedicalRecordNumber = medicalRecordNumber,
                            Reason = reason,
                            TtlMinutes = ttlMinutes
                        },
                        transaction);

                    // The event exists if and only if the grant committed. The reason stays in the grant row.
                    if (row?.GrantCode is not null)
                        await _activityLogger.LogAsync(_connection, transaction, new ActivityEvent(
                            ActivityEventTypes.BreakGlassAccess,
                            userCode,
                            ActivityEntityTypes.Patient,
                            row.PatientCode,
                            "Break-glass access granted",
                            new { GrantCode = row.GrantCode, row.ExpiresAt }));

                    return row;
                },
                userCode),
            errorFactory: PatientAccessRepositoryError (ex) => new GrantBreakGlassError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<BreakGlassReportRow>, PatientAccessRepositoryError>> GetBreakGlassReportAsync(
        DateTimeOffset from, DateTimeOffset toExclusive) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, BreakGlassReportRow>(
                _connection,
                PatientAccessRepositorySql.GetBreakGlassReport,
                new { From = from, ToExclusive = toExclusive }),
            errorFactory: PatientAccessRepositoryError (ex) => new GetBreakGlassReportError(ex.Message, ex)
        );
}
