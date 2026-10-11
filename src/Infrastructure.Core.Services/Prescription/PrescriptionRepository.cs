using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Prescription;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Models.Prescription;

namespace Infrastructure.Core.Services.Prescription;

public sealed class PrescriptionRepository : BaseDatabaseService, IPrescriptionRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;
    private readonly IActivityLogger _activityLogger;

    public PrescriptionRepository(IDbConnection connection, IUserContext userContext,
        IActivityLogger activityLogger)
    {
        _connection = connection;
        _userContext = userContext;
        _activityLogger = activityLogger;
    }

    private Task<Result<int, PrescriptionRepositoryError>> ChangeStatusAsync(
        string sql, Guid code, Guid performedByUserCode, string eventType, string summary) =>
        Result.TryAsync(
            operation: () => ExecuteAuditedWriteAsync(
                _connection,
                _userContext,
                async transaction =>
                {
                    int affected = await ExecuteNonQueryAsync(
                        _connection,
                        sql,
                        new { Code = code, PerformedByUserCode = performedByUserCode },
                        transaction);

                    if (affected > 0)
                        await LogEventAsync(transaction, eventType, performedByUserCode, code, summary);

                    return affected;
                },
                performedByUserCode),
            errorFactory: PrescriptionRepositoryError (ex) => new ChangeStatusPrescriptionError(ex.Message, ex)
        );

    private Task LogEventAsync(IDbTransaction transaction, string eventType, Guid actorUserCode, Guid prescriptionCode,
        string summary) =>
        _activityLogger.LogAsync(_connection, transaction,
            new ActivityEvent(eventType, actorUserCode, ActivityEntityTypes.Prescription, prescriptionCode, summary));

    /// <inheritdoc/>
    public async Task<Result<PrescriptionRow?, PrescriptionRepositoryError>> InsertAsync(
        Guid diagnosticCode, string? notes, DateTime validUntil, string detailsJson, Guid createdByUserCode) =>
        await Result.TryAsync(
            operation: () => ExecuteAuditedWriteAsync(
                _connection,
                _userContext,
                async transaction =>
                {
                    PrescriptionRow? row = await ExecuteFirstOrDefaultAsync<object, PrescriptionRow>(
                        _connection,
                        PrescriptionRepositorySql.Insert,
                        new
                        {
                            DiagnosticCode = diagnosticCode, Notes = notes, ValidUntil = validUntil,
                            DetailsJson = detailsJson, CreatedByUserCode = createdByUserCode
                        },
                        transaction);

                    if (row is not null)
                        await LogEventAsync(transaction, ActivityEventTypes.PrescriptionCreated, createdByUserCode,
                            row.PrescriptionCode, "Prescription created");

                    return row;
                },
                createdByUserCode),
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
            await SetAuditContextAsync(_connection, _userContext, transaction);
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
    public Task<Result<int, PrescriptionRepositoryError>> SignAsync(
        Guid code, Guid performedByUserCode) =>
        ChangeStatusAsync(PrescriptionRepositorySql.Sign, code, performedByUserCode,
            ActivityEventTypes.PrescriptionSigned, "Prescription signed");

    /// <inheritdoc/>
    public Task<Result<int, PrescriptionRepositoryError>> SuspendAsync(
        Guid code, Guid performedByUserCode) =>
        ChangeStatusAsync(PrescriptionRepositorySql.Suspend, code, performedByUserCode,
            ActivityEventTypes.PrescriptionSuspended, "Prescription suspended");

    /// <inheritdoc/>
    public Task<Result<int, PrescriptionRepositoryError>> ReactivateAsync(
        Guid code, Guid performedByUserCode) =>
        ChangeStatusAsync(PrescriptionRepositorySql.Reactivate, code, performedByUserCode,
            ActivityEventTypes.PrescriptionReactivated, "Prescription reactivated");

    /// <inheritdoc/>
    public Task<Result<int, PrescriptionRepositoryError>> CancelAsync(
        Guid code, Guid performedByUserCode) =>
        ChangeStatusAsync(PrescriptionRepositorySql.Cancel, code, performedByUserCode,
            ActivityEventTypes.PrescriptionCancelled, "Prescription cancelled");

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<DoctorDraftPrescriptionRow>, PrescriptionRepositoryError>>
        GetDoctorDraftPrescriptionsAsync(
            Guid doctorUserCode) =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<DoctorDraftPrescriptionRow>)(await _connection
                    .QueryAsync<DoctorDraftPrescriptionRow>(
                        PrescriptionRepositorySql.GetDoctorDraftPrescriptions,
                        new { DoctorUserCode = doctorUserCode }))
                .AsList(),
            errorFactory: PrescriptionRepositoryError (ex) => new GetDoctorDraftPrescriptionsError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<NurseDispensationRow>, PrescriptionRepositoryError>>
        GetNurseDispensationsByDateAsync(
            Guid nurseUserCode, DateTime date) =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<NurseDispensationRow>)(await _connection
                    .QueryAsync<NurseDispensationRow>(
                        PrescriptionRepositorySql.GetNurseDispensationsByDate,
                        new { NurseUserCode = nurseUserCode, Date = date }))
                .AsList(),
            errorFactory: PrescriptionRepositoryError (ex) => new GetNurseDispensationsError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public Task<Result<int, PrescriptionRepositoryError>> DispenseAsync(
        Guid code, Guid performedByUserCode) =>
        ChangeStatusAsync(PrescriptionRepositorySql.Dispense, code, performedByUserCode,
            ActivityEventTypes.PrescriptionDispensed, "Prescription dispensed");
}