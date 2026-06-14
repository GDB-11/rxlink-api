using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Prescription;
using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Models.Prescription;

namespace Infrastructure.Core.Services.Prescription;

public sealed class PrescriptionRepository : BaseDatabaseService, IPrescriptionRepository
{
    private readonly IDbConnection _connection;

    public PrescriptionRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> InsertAsync(
        Guid diagnosticCode, string? notes, DateTime validUntil, string detailsJson, Guid createdByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PrescriptionRow>(
                _connection,
                PrescriptionRepositorySql.Insert,
                new
                {
                    DiagnosticCode = diagnosticCode, Notes = notes, ValidUntil = validUntil, DetailsJson = detailsJson,
                    CreatedByUserCode = createdByUserCode
                }),
            errorFactory: PrescriptionRepositoryError (ex) =>
                ex.Message.Contains("uq_prescription_diagnostic") || ex.Message.Contains("23505")
                    ? new InsertPrescriptionDuplicateError(ex.Message, ex)
                    : new InsertPrescriptionError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> GetByCodeAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, PrescriptionRow>(
                _connection,
                PrescriptionRepositorySql.GetByCode,
                new { Code = code }),
            errorFactory: PrescriptionRepositoryError (ex) => new GetPrescriptionError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> UpdateAsync(
        Guid code, string? notes, DateTime validUntil, string detailsJson, Guid modifiedByUserCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            string? statusName = await _connection.ExecuteScalarAsync<string?>(
                PrescriptionRepositorySql.GetStatusNameByCode,
                new { Code = code },
                transaction
            );

            if (statusName is null)
            {
                transaction.Rollback();
                return Result<PrescriptionRow?, PrescriptionRepositoryError>.Success(null);
            }

            if (statusName != "Borrador")
            {
                transaction.Rollback();
                return Result<PrescriptionRow?, PrescriptionRepositoryError>.Failure(
                    new UpdatePrescriptionInvalidStatusError($"La receta está en estado '{statusName}'."));
            }

            PrescriptionRow? row = await _connection.QueryFirstOrDefaultAsync<PrescriptionRow>(
                PrescriptionRepositorySql.Update,
                new
                {
                    Code = code, Notes = notes, ValidUntil = validUntil, DetailsJson = detailsJson,
                    ModifiedByUserCode = modifiedByUserCode
                },
                transaction
            );

            transaction.Commit();
            return Result<PrescriptionRow?, PrescriptionRepositoryError>.Success(row);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<PrescriptionRow?, PrescriptionRepositoryError>.Failure(
                new UpdatePrescriptionError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int, PrescriptionRepositoryError>> SignAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PrescriptionRepositorySql.Sign,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PrescriptionRepositoryError (ex) => new ChangeStatusPrescriptionError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PrescriptionRepositoryError>> SuspendAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PrescriptionRepositorySql.Suspend,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PrescriptionRepositoryError (ex) => new ChangeStatusPrescriptionError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PrescriptionRepositoryError>> ReactivateAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PrescriptionRepositorySql.Reactivate,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PrescriptionRepositoryError (ex) => new ChangeStatusPrescriptionError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PrescriptionRepositoryError>> CancelAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PrescriptionRepositorySql.Cancel,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PrescriptionRepositoryError (ex) => new ChangeStatusPrescriptionError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, PrescriptionRepositoryError>> DispenseAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                PrescriptionRepositorySql.Dispense,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: PrescriptionRepositoryError (ex) => new ChangeStatusPrescriptionError(ex.Message, ex)
        );
}