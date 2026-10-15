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
        string summary, object? payload = null) =>
        _activityLogger.LogAsync(_connection, transaction,
            new ActivityEvent(eventType, actorUserCode, ActivityEntityTypes.Prescription, prescriptionCode, summary,
                payload));

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
    public async Task<Result<IReadOnlyList<ActivePatientMedicationRow>, PrescriptionRepositoryError>>
        GetActiveMedicationsOfPatientAsync(Guid patientCode, Guid excludePrescriptionCode) =>
        await Result.TryAsync(
            operation: async () => (IReadOnlyList<ActivePatientMedicationRow>)(await _connection
                    .QueryAsync<ActivePatientMedicationRow>(
                        PrescriptionRepositorySql.GetActiveMedicationsOfPatient,
                        new { PatientCode = patientCode, ExcludePrescriptionCode = excludePrescriptionCode }))
                .AsList(),
            errorFactory: PrescriptionRepositoryError (ex) => new GetActiveMedicationsError(ex.Message, ex)
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
        Guid code, Guid performedByUserCode, IReadOnlyList<DispenseLineRow> lines, string? overrideReason) =>
        Result.TryAsync(
            operation: () => ExecuteAuditedWriteAsync(
                _connection,
                _userContext,
                async transaction =>
                {
                    int affected = await ExecuteNonQueryAsync(
                        _connection,
                        PrescriptionRepositorySql.Dispense,
                        new { Code = code, PerformedByUserCode = performedByUserCode },
                        transaction);

                    if (affected == 0)
                        return 0;

                    await MoveStockAsync(transaction, code, performedByUserCode, lines, overrideReason);
                    await LogEventAsync(transaction, ActivityEventTypes.PrescriptionDispensed, performedByUserCode,
                        code, "Prescription dispensed");

                    return affected;
                },
                performedByUserCode),
            errorFactory: PrescriptionRepositoryError (ex) => ex switch
            {
                DispenseLinesInvalidException => new DispenseLinesInvalidError(ex.Message, ex),
                InsufficientStockException => new InsufficientStockError(ex.Message, ex),
                _ => new ChangeStatusPrescriptionError(ex.Message, ex)
            }
        );

    /// <summary>
    /// Writes a <c>Dispense</c> movement per line and decrements the balance. Stock rows are locked in
    /// medication-code order so two dispensations touching the same medications cannot deadlock.
    /// Throws (rolling the whole transaction back, status change included) when the lines don't match the
    /// prescription or a balance is too low.
    /// </summary>
    private async Task MoveStockAsync(
        IDbTransaction transaction, Guid prescriptionCode, Guid performedByUserCode,
        IReadOnlyList<DispenseLineRow> lines, string? overrideReason)
    {
        List<DispenseDetailRow> details = (await _connection.QueryAsync<DispenseDetailRow>(
            PrescriptionRepositorySql.GetDispenseDetails, new { Code = prescriptionCode }, transaction)).AsList();

        bool matches = lines.Count == details.Count
                       && lines.Select(l => l.PrescriptionDetailCode).Distinct().Count() == details.Count
                       && lines.All(l => l.Quantity > 0)
                       && details.All(d => lines.Any(l => l.PrescriptionDetailCode == d.PrescriptionDetailCode));
        if (!matches)
            throw new DispenseLinesInvalidException();

        int clinicId = await _connection.ExecuteScalarAsync<int>(
            PrescriptionRepositorySql.GetDefaultClinicId, transaction: transaction);

        var movements = details
            .Select(d => (Detail: d, Quantity: lines.First(l => l.PrescriptionDetailCode == d.PrescriptionDetailCode).Quantity))
            .ToList();

        foreach (IGrouping<Guid, (DispenseDetailRow Detail, decimal Quantity)> medication in movements
                     .GroupBy(m => m.Detail.MedicationCode).OrderBy(g => g.Key))
        {
            decimal required = medication.Sum(m => m.Quantity);
            decimal? balance = await _connection.ExecuteScalarAsync<decimal?>(
                PrescriptionRepositorySql.LockClinicStock,
                new { ClinicId = clinicId, MedicationCode = medication.Key }, transaction);

            if ((balance ?? 0) < required)
            {
                if (overrideReason is null)
                    throw new InsufficientStockException();

                if (balance is null)
                {
                    await _connection.ExecuteAsync(
                        PrescriptionRepositorySql.EnsureClinicStock,
                        new { ClinicId = clinicId, MedicationCode = medication.Key }, transaction);
                    balance = await _connection.ExecuteScalarAsync<decimal>(
                        PrescriptionRepositorySql.LockClinicStock,
                        new { ClinicId = clinicId, MedicationCode = medication.Key }, transaction);
                }
            }

            // Whatever the balance covers is taken line by line; the rest of the lines is the shortfall.
            decimal covered = Math.Max(balance ?? 0, 0);
            decimal shortfallTotal = Math.Max(required - covered, 0);
            if (shortfallTotal > 0)
                await LogEventAsync(transaction, ActivityEventTypes.StockOverrideDispense, performedByUserCode,
                    prescriptionCode, "Stock override on dispense", new
                    {
                        medicationCode = medication.Key, balance, required, shortfall = shortfallTotal,
                        reason = overrideReason
                    });

            await _connection.ExecuteAsync(
                PrescriptionRepositorySql.DecrementClinicStock,
                new
                {
                    ClinicId = clinicId, MedicationCode = medication.Key, Quantity = required,
                    PerformedByUserCode = performedByUserCode
                }, transaction);

            foreach ((DispenseDetailRow detail, decimal quantity) in medication)
            {
                decimal fromStock = Math.Min(quantity, covered);
                covered -= fromStock;
                decimal shortfall = quantity - fromStock;

                await _connection.ExecuteAsync(
                    PrescriptionRepositorySql.InsertDispenseMovement,
                    new
                    {
                        ClinicId = clinicId, MedicationCode = medication.Key, Quantity = quantity,
                        detail.PrescriptionDetailId, PerformedByUserCode = performedByUserCode,
                        ShortfallQuantity = shortfall > 0 ? shortfall : (decimal?)null,
                        Reason = shortfall > 0 ? overrideReason : null
                    }, transaction);
            }
        }
    }

    // Control-flow only: thrown inside the transaction so it rolls back, translated by errorFactory.
    private sealed class DispenseLinesInvalidException : Exception;

    private sealed class InsufficientStockException : Exception;
}
