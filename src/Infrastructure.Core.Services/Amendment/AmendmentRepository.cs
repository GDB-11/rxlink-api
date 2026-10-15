using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Amendment;
using Infrastructure.Core.Interfaces.Amendment;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Models.Amendment;

namespace Infrastructure.Core.Services.Amendment;

public sealed class AmendmentRepository : BaseDatabaseService, IAmendmentRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public AmendmentRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<AmendmentRow?, AmendmentRepositoryError>> InsertAsync(
        Guid patientCode, string targetArea, string requestedChange) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, AmendmentRow>(
                _connection, _userContext, AmendmentRepositorySql.Insert,
                new { PatientCode = patientCode, TargetArea = targetArea, RequestedChange = requestedChange }),
            errorFactory: AmendmentRepositoryError (ex) => new InsertAmendmentError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<AmendmentRow?, AmendmentRepositoryError>> GetByCodeAsync(Guid amendmentRequestCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, AmendmentRow>(
                _connection, AmendmentRepositorySql.GetByCode,
                new { AmendmentRequestCode = amendmentRequestCode }),
            errorFactory: AmendmentRepositoryError (ex) => new GetAmendmentError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AmendmentRow>, AmendmentRepositoryError>> GetPageByPatientAsync(
        Guid patientCode, int offset, int limit) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, AmendmentRow>(
                _connection, AmendmentRepositorySql.GetPageByPatient,
                new { PatientCode = patientCode, Offset = offset, Limit = limit }),
            errorFactory: AmendmentRepositoryError (ex) => new GetAmendmentError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AmendmentRow>, AmendmentRepositoryError>> GetPageAsync(
        string? status, int offset, int limit) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, AmendmentRow>(
                _connection, AmendmentRepositorySql.GetPage,
                new { Status = status, Offset = offset, Limit = limit }),
            errorFactory: AmendmentRepositoryError (ex) => new GetAmendmentError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<AmendmentRow?, AmendmentRepositoryError>> DecideAsync(
        Guid amendmentRequestCode, bool accepted, string reason, string? amendmentText, Guid reviewerUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, AmendmentRow>(
                _connection, _userContext, AmendmentRepositorySql.Decide,
                new
                {
                    AmendmentRequestCode = amendmentRequestCode,
                    Accepted = accepted,
                    Reason = reason,
                    AmendmentText = amendmentText,
                    ReviewerUserCode = reviewerUserCode
                }),
            errorFactory: AmendmentRepositoryError (ex) => new DecideAmendmentError(ex.Message, ex)
        );
}
