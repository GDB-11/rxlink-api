using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Disclosure;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Disclosure;
using Infrastructure.Core.Models.Disclosure;

namespace Infrastructure.Core.Services.Disclosure;

public sealed class DisclosureRepository : BaseDatabaseService, IDisclosureRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public DisclosureRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<DisclosureRow?, DisclosureRepositoryError>> InsertAsync(
        Guid patientCode, DateTimeOffset disclosedAt, string recipientName, string recipientType,
        string purpose, string description, Guid recordedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, DisclosureRow>(
                _connection, _userContext, DisclosureRepositorySql.Insert,
                new
                {
                    PatientCode = patientCode,
                    DisclosedAt = disclosedAt,
                    RecipientName = recipientName,
                    RecipientType = recipientType,
                    Purpose = purpose,
                    Description = description,
                    RecordedByUserCode = recordedByUserCode
                }),
            errorFactory: DisclosureRepositoryError (ex) => new InsertDisclosureError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<DisclosureRow>, DisclosureRepositoryError>> GetPageAsync(
        Guid patientCode, int offset, int limit) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, DisclosureRow>(
                _connection, DisclosureRepositorySql.GetPage,
                new { PatientCode = patientCode, Offset = offset, Limit = limit }),
            errorFactory: DisclosureRepositoryError (ex) => new GetDisclosureError(ex.Message, ex)
        );
}
