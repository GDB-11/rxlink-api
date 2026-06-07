using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Diagnostic;
using Infrastructure.Core.Interfaces.Diagnostic;
using Infrastructure.Core.Models.Diagnostic;

namespace Infrastructure.Core.Services.Diagnostic;

public sealed class DiagnosticRepository : BaseDatabaseService, IDiagnosticRepository
{
    private readonly IDbConnection _connection;

    public DiagnosticRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<DiagnosticRow>, DiagnosticRepositoryError>> GetPageAsync(
        Guid patientCode, int offset, int limit) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, DiagnosticRow>(
                _connection,
                DiagnosticRepositorySql.GetPage,
                new { PatientCode = patientCode, Offset = offset, Limit = limit }),
            errorFactory: DiagnosticRepositoryError (ex) => new GetDiagnosticsPageError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<DiagnosticRow?, DiagnosticRepositoryError>> InsertAsync(
        Guid patientCode, string description, DateOnly diagnosedAt, string? notes, Guid createdByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, DiagnosticRow>(
                _connection,
                DiagnosticRepositorySql.Insert,
                new
                {
                    PatientCode        = patientCode,
                    Description        = description,
                    DiagnosedAt        = diagnosedAt,
                    Notes              = notes,
                    CreatedByUserCode  = createdByUserCode
                }),
            errorFactory: DiagnosticRepositoryError (ex) => new InsertDiagnosticError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<DiagnosticRow?, DiagnosticRepositoryError>> UpdateAsync(
        Guid code, string description, DateOnly diagnosedAt, string? notes, Guid modifiedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, DiagnosticRow>(
                _connection,
                DiagnosticRepositorySql.Update,
                new
                {
                    Code               = code,
                    Description        = description,
                    DiagnosedAt        = diagnosedAt,
                    Notes              = notes,
                    ModifiedByUserCode = modifiedByUserCode
                }),
            errorFactory: DiagnosticRepositoryError (ex) => new UpdateDiagnosticError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, DiagnosticRepositoryError>> DeactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                DiagnosticRepositorySql.Deactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: DiagnosticRepositoryError (ex) => new DeactivateDiagnosticError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, DiagnosticRepositoryError>> ActivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                DiagnosticRepositorySql.Activate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: DiagnosticRepositoryError (ex) => new DeactivateDiagnosticError(ex.Message, ex)
        );
}
