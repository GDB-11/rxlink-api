using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Models.Appointment;

namespace Infrastructure.Core.Services.Appointment;

public sealed class AppointmentRepository : BaseDatabaseService, IAppointmentRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;
    private readonly IActivityLogger _activityLogger;

    public AppointmentRepository(IDbConnection connection, IUserContext userContext,
        IActivityLogger activityLogger)
    {
        _connection = connection;
        _userContext = userContext;
        _activityLogger = activityLogger;
    }

    private Task LogEventAsync(IDbTransaction transaction, string eventType, Guid appointmentCode, string summary,
        Guid? actorUserCode = null, object? payload = null) =>
        _activityLogger.LogAsync(_connection, transaction,
            new ActivityEvent(eventType, actorUserCode ?? _userContext.UserCode, ActivityEntityTypes.Appointment,
                appointmentCode, summary, payload));

    /// <inheritdoc/>
    public async Task<Result<AppointmentRow?, AppointmentRepositoryError>> InsertAsync(
        Guid patientCode,
        Guid availabilityCode,
        Guid consultationTypeCode,
        bool payNow,
        InsuranceTerms? insurance)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            await SetAuditContextAsync(_connection, _userContext, transaction);
            int? patientId = await _connection.ExecuteScalarAsync<int?>(
                AppointmentRepositorySql.GetPatientId,
                new { PatientCode = patientCode },
                transaction);

            if (patientId is null)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertPatientNotFoundError());
            }

            SlotRow? slot = await _connection.QueryFirstOrDefaultAsync<SlotRow>(
                AppointmentRepositorySql.GetAvailabilitySlot,
                new { AvailabilityCode = availabilityCode },
                transaction);

            if (slot is null || slot.DeletedAt is not null)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotNotFoundError());
            }

            if (slot.IsBooked)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotAlreadyBookedError());
            }

            if (slot.Date < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotExpiredError());
            }

            int? consultationTypeId = await _connection.ExecuteScalarAsync<int?>(
                AppointmentRepositorySql.GetConsultationTypeId,
                new { ConsultationTypeCode = consultationTypeCode },
                transaction);

            if (consultationTypeId is null)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertConsultationTypeNotFoundError());
            }

            int lockRowsAffected = await _connection.ExecuteAsync(
                AppointmentRepositorySql.LockSlot,
                new { slot.DoctorAvailabilityId },
                transaction);

            if (lockRowsAffected == 0)
            {
                transaction.Rollback();
                // Race condition: slot was booked between our SELECT and UPDATE
                return Result<AppointmentRow?, AppointmentRepositoryError>.Success(null);
            }

            DateTimeOffset scheduledAt = new DateTimeOffset(
                slot.Date.ToDateTime(slot.StartTime, DateTimeKind.Utc));

            NewAppointmentIdentity identity = await _connection.QueryFirstAsync<NewAppointmentIdentity>(
                AppointmentRepositorySql.InsertAppointment,
                new
                {
                    PatientId = patientId.Value,
                    slot.DoctorId,
                    slot.DoctorAvailabilityId,
                    ConsultationTypeId = consultationTypeId.Value,
                    ScheduledAt = scheduledAt
                },
                transaction);

            if (payNow)
            {
                Result<Unit, AppointmentRepositoryError> paymentResult = await ResolveAndRecordPaymentAsync(
                    identity.AppointmentId, slot.DoctorId, consultationTypeId.Value, insurance,
                    recordedByUserCode: null, transaction);

                if (!paymentResult.IsSuccess)
                {
                    transaction.Rollback();
                    return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(paymentResult.Error!);
                }

                await _connection.ExecuteAsync(
                    AppointmentRepositorySql.ConfirmPaymentByAdmin,
                    new { Code = identity.AppointmentCode },
                    transaction);
            }

            await LogEventAsync(transaction, ActivityEventTypes.AppointmentCreated, identity.AppointmentCode,
                "Appointment created", null, new { payNow });

            if (payNow)
                await LogEventAsync(transaction, ActivityEventTypes.AppointmentPaymentConfirmed,
                    identity.AppointmentCode, "Appointment payment confirmed", null);

            AppointmentRow? row = await _connection.QueryFirstOrDefaultAsync<AppointmentRow>(
                AppointmentRepositorySql.GetByCode,
                new { Code = identity.AppointmentCode },
                transaction);

            transaction.Commit();
            return Result<AppointmentRow?, AppointmentRepositoryError>.Success(row);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                new InsertAppointmentError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<AppointmentRow?, AppointmentRepositoryError>> GetByCodeAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, AppointmentRow>(
                _connection,
                AppointmentRepositorySql.GetByCode,
                new { Code = code }),
            errorFactory: AppointmentRepositoryError (ex) => new GetAppointmentError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> ConfirmPaymentAsync(
        Guid code, Guid patientCode, InsuranceTerms? insurance)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            await SetAuditContextAsync(_connection, _userContext, transaction);
            PricingContextRow? pricing = await _connection.QueryFirstOrDefaultAsync<PricingContextRow>(
                AppointmentRepositorySql.GetAppointmentPricingContext,
                new { Code = code },
                transaction);

            if (pricing is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            if (pricing.BaseAmount is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Failure(new InsertSpecialtyPriceNotFoundError());
            }

            Result<Unit, AppointmentRepositoryError> paymentResult = await ResolveAndRecordPaymentAsync(
                pricing.AppointmentId, pricing.BaseAmount.Value, insurance, recordedByUserCode: null, transaction);

            if (!paymentResult.IsSuccess)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Failure(paymentResult.Error!);
            }

            int rows = await _connection.ExecuteAsync(
                AppointmentRepositorySql.ConfirmPayment,
                new { Code = code, PatientCode = patientCode },
                transaction);

            if (rows == 0)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            await LogEventAsync(transaction, ActivityEventTypes.AppointmentPaymentConfirmed, code, "Appointment payment confirmed");

            transaction.Commit();
            return Result<int, AppointmentRepositoryError>.Success(rows);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<int, AppointmentRepositoryError>.Failure(
                new TransitionAppointmentError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> CancelAsync(
        Guid code, Guid? patientCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            await SetAuditContextAsync(_connection, _userContext, transaction);
            CancellableRow? target = patientCode.HasValue
                ? await _connection.QueryFirstOrDefaultAsync<CancellableRow>(
                    AppointmentRepositorySql.GetCancellableByPatient,
                    new { Code = code, PatientCode = patientCode.Value },
                    transaction)
                : await _connection.QueryFirstOrDefaultAsync<CancellableRow>(
                    AppointmentRepositorySql.GetCancellableByAdmin,
                    new { Code = code },
                    transaction);

            if (target is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            await _connection.ExecuteAsync(
                AppointmentRepositorySql.ReleaseSlot,
                new { target.DoctorAvailabilityId },
                transaction);

            int rows = await _connection.ExecuteAsync(
                AppointmentRepositorySql.SetStatusCancelled,
                new { target.AppointmentId },
                transaction);

            if (rows > 0)
                await LogEventAsync(transaction, ActivityEventTypes.AppointmentCancelled, code, "Appointment cancelled");

            transaction.Commit();
            return Result<int, AppointmentRepositoryError>.Success(rows);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<int, AppointmentRepositoryError>.Failure(
                new TransitionAppointmentError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> CompleteAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedNonQueryAsync(_connection, _userContext,
                AppointmentRepositorySql.Complete,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: AppointmentRepositoryError (ex) => new TransitionAppointmentError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> NoShowAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: () => ExecuteAuditedWriteAsync(
                _connection,
                _userContext,
                async transaction =>
                {
                    int rows = await ExecuteNonQueryAsync(
                        _connection,
                        AppointmentRepositorySql.NoShow,
                        new { Code = code, PerformedByUserCode = performedByUserCode },
                        transaction);

                    if (rows > 0)
                        await LogEventAsync(transaction, ActivityEventTypes.AppointmentNoShow, code,
                            "Appointment marked as no-show", performedByUserCode);

                    return rows;
                },
                performedByUserCode),
            errorFactory: AppointmentRepositoryError (ex) => new TransitionAppointmentError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>>
        GetPatientAppointmentsAsync(Guid patientCode, int page, int pageSize) =>
        await Result.TryAsync(
            operation: async () =>
            {
                int offset = (page - 1) * pageSize;
                IEnumerable<AppointmentRow> rows = await ExecuteQueryAsync<object, AppointmentRow>(
                    _connection,
                    AppointmentRepositorySql.GetPatientAppointments,
                    new { PatientCode = patientCode, PageSize = pageSize, Offset = offset });

                AppointmentRow[] array = rows as AppointmentRow[] ?? rows.ToArray();
                int total = array.Length > 0 ? (int)array[0].TotalCount : 0;
                return (Items: (IEnumerable<AppointmentRow>)array, Total: total);
            },
            errorFactory: AppointmentRepositoryError (ex) => new GetPatientAppointmentsError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>>
        GetDoctorAppointmentsAsync(Guid doctorUserCode, int page, int pageSize, DateTime? date, string? statusName) =>
        await Result.TryAsync(
            operation: async () =>
            {
                int offset = (page - 1) * pageSize;
                IEnumerable<AppointmentRow> rows = await ExecuteQueryAsync<object, AppointmentRow>(
                    _connection,
                    AppointmentRepositorySql.GetDoctorAppointments,
                    new
                    {
                        DoctorCode = doctorUserCode, Date = date, StatusName = statusName, PageSize = pageSize,
                        Offset = offset
                    });

                AppointmentRow[] array = rows as AppointmentRow[] ?? rows.ToArray();
                int total = array.Length > 0 ? (int)array[0].TotalCount : 0;
                return (Items: (IEnumerable<AppointmentRow>)array, Total: total);
            },
            errorFactory: AppointmentRepositoryError (ex) => new GetDoctorAppointmentsError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<AppointmentRow?, AppointmentRepositoryError>> InsertByAdminAsync(
        Guid patientCode,
        Guid availabilityCode,
        Guid consultationTypeCode,
        bool payNow,
        InsuranceTerms? insurance,
        Guid? recordedByUserCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            await SetAuditContextAsync(_connection, _userContext, transaction);
            int? foundPatientId = await _connection.ExecuteScalarAsync<int?>(
                AppointmentRepositorySql.GetPatientId,
                new { PatientCode = patientCode },
                transaction);

            if (foundPatientId is null)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertPatientNotFoundError());
            }

            SlotRow? slot = await _connection.QueryFirstOrDefaultAsync<SlotRow>(
                AppointmentRepositorySql.GetAvailabilitySlot,
                new { AvailabilityCode = availabilityCode },
                transaction);

            if (slot is null || slot.DeletedAt is not null)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotNotFoundError());
            }

            if (slot.IsBooked)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotAlreadyBookedError());
            }

            if (slot.Date < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertSlotExpiredError());
            }

            int? consultationTypeId = await _connection.ExecuteScalarAsync<int?>(
                AppointmentRepositorySql.GetConsultationTypeId,
                new { ConsultationTypeCode = consultationTypeCode },
                transaction);

            if (consultationTypeId is null)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                    new InsertConsultationTypeNotFoundError());
            }

            int lockRowsAffected = await _connection.ExecuteAsync(
                AppointmentRepositorySql.LockSlot,
                new { slot.DoctorAvailabilityId },
                transaction);

            if (lockRowsAffected == 0)
            {
                transaction.Rollback();
                return Result<AppointmentRow?, AppointmentRepositoryError>.Success(null);
            }

            DateTimeOffset scheduledAt = new DateTimeOffset(
                slot.Date.ToDateTime(slot.StartTime, DateTimeKind.Utc));

            NewAppointmentIdentity identity = await _connection.QueryFirstAsync<NewAppointmentIdentity>(
                AppointmentRepositorySql.InsertAppointment,
                new
                {
                    PatientId = foundPatientId.Value,
                    slot.DoctorId,
                    slot.DoctorAvailabilityId,
                    ConsultationTypeId = consultationTypeId.Value,
                    ScheduledAt = scheduledAt
                },
                transaction);

            if (payNow)
            {
                Result<Unit, AppointmentRepositoryError> paymentResult = await ResolveAndRecordPaymentAsync(
                    identity.AppointmentId, slot.DoctorId, consultationTypeId.Value, insurance,
                    recordedByUserCode, transaction);

                if (!paymentResult.IsSuccess)
                {
                    transaction.Rollback();
                    return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(paymentResult.Error!);
                }

                await _connection.ExecuteAsync(
                    AppointmentRepositorySql.ConfirmPaymentByAdmin,
                    new { Code = identity.AppointmentCode },
                    transaction);
            }

            await LogEventAsync(transaction, ActivityEventTypes.AppointmentCreated, identity.AppointmentCode,
                "Appointment created", recordedByUserCode, new { payNow });

            if (payNow)
                await LogEventAsync(transaction, ActivityEventTypes.AppointmentPaymentConfirmed,
                    identity.AppointmentCode, "Appointment payment confirmed", recordedByUserCode);

            AppointmentRow? row = await _connection.QueryFirstOrDefaultAsync<AppointmentRow>(
                AppointmentRepositorySql.GetByCode,
                new { Code = identity.AppointmentCode },
                transaction);

            transaction.Commit();
            return Result<AppointmentRow?, AppointmentRepositoryError>.Success(row);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<AppointmentRow?, AppointmentRepositoryError>.Failure(
                new InsertAppointmentError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> ConfirmPaymentByAdminAsync(
        Guid code, InsuranceTerms? insurance, Guid recordedByUserCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            await SetAuditContextAsync(_connection, _userContext, transaction);
            PricingContextRow? pricing = await _connection.QueryFirstOrDefaultAsync<PricingContextRow>(
                AppointmentRepositorySql.GetAppointmentPricingContext,
                new { Code = code },
                transaction);

            if (pricing is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            if (pricing.BaseAmount is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Failure(new InsertSpecialtyPriceNotFoundError());
            }

            Result<Unit, AppointmentRepositoryError> paymentResult = await ResolveAndRecordPaymentAsync(
                pricing.AppointmentId, pricing.BaseAmount.Value, insurance, recordedByUserCode, transaction);

            if (!paymentResult.IsSuccess)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Failure(paymentResult.Error!);
            }

            int rows = await _connection.ExecuteAsync(
                AppointmentRepositorySql.ConfirmPaymentByAdmin,
                new { Code = code },
                transaction);

            if (rows == 0)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            await LogEventAsync(transaction, ActivityEventTypes.AppointmentPaymentConfirmed, code, "Appointment payment confirmed", recordedByUserCode);

            transaction.Commit();
            return Result<int, AppointmentRepositoryError>.Success(rows);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<int, AppointmentRepositoryError>.Failure(
                new AdminConfirmPaymentError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> RevertPaymentAsync(Guid code)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            await SetAuditContextAsync(_connection, _userContext, transaction);
            int rows = await _connection.ExecuteAsync(
                AppointmentRepositorySql.RevertPayment,
                new { Code = code },
                transaction);

            if (rows == 0)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            await _connection.ExecuteAsync(
                AppointmentRepositorySql.DeleteAppointmentPaymentByCode,
                new { Code = code },
                transaction);

            transaction.Commit();
            return Result<int, AppointmentRepositoryError>.Success(rows);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return Result<int, AppointmentRepositoryError>.Failure(
                new RevertPaymentError(ex.Message, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<(IEnumerable<AppointmentRow> Items, int Total), AppointmentRepositoryError>>
        GetAdminAppointmentsAsync(int page, int pageSize, string? patientSearch, DateTime? date, string? statusName) =>
        await Result.TryAsync(
            operation: async () =>
            {
                int offset = (page - 1) * pageSize;
                IEnumerable<AppointmentRow> rows = await ExecuteQueryAsync<object, AppointmentRow>(
                    _connection,
                    AppointmentRepositorySql.GetAdminAppointments,
                    new
                    {
                        PatientSearch = patientSearch,
                        Date = date,
                        StatusName = statusName,
                        PageSize = pageSize,
                        Offset = offset
                    });

                AppointmentRow[] array = rows as AppointmentRow[] ?? rows.ToArray();
                int total = array.Length > 0 ? (int)array[0].TotalCount : 0;
                return (Items: (IEnumerable<AppointmentRow>)array, Total: total);
            },
            errorFactory: AppointmentRepositoryError (ex) => new GetAdminAppointmentsError(ex.Message, ex));

    /// <summary>
    /// Resolves the base price for a doctor/consultation-type pair from the partner's specialty prices and
    /// inserts the AppointmentPayment snapshot with the given insurer terms. Returns a typed failure when no price
    /// exists for the doctor's specialty — nothing is written in that case.
    /// </summary>
    private async Task<Result<Unit, AppointmentRepositoryError>> ResolveAndRecordPaymentAsync(
        int appointmentId, int doctorId, int consultationTypeId, InsuranceTerms? insurance,
        Guid? recordedByUserCode, IDbTransaction transaction)
    {
        decimal? baseAmount = await _connection.ExecuteScalarAsync<decimal?>(
            AppointmentRepositorySql.GetBasePriceForBooking,
            new { DoctorId = doctorId, ConsultationTypeId = consultationTypeId },
            transaction);

        if (baseAmount is null)
            return Result<Unit, AppointmentRepositoryError>.Failure(new InsertSpecialtyPriceNotFoundError());

        return await ResolveAndRecordPaymentAsync(
            appointmentId, baseAmount.Value, insurance, recordedByUserCode, transaction);
    }

    /// <summary>
    /// Inserts the AppointmentPayment snapshot for an already-known base price. The coverage, insurer code and
    /// insurer name come from <paramref name="insurance"/> (resolved by the service); a payment is a historical
    /// record, so they are stored as given and never re-resolved.
    /// </summary>
    private async Task<Result<Unit, AppointmentRepositoryError>> ResolveAndRecordPaymentAsync(
        int appointmentId, decimal baseAmount, InsuranceTerms? insurance,
        Guid? recordedByUserCode, IDbTransaction transaction)
    {
        decimal coveragePercentage = insurance?.CoveragePercentage ?? 0m;
        decimal coveredAmount = Math.Round(baseAmount * coveragePercentage / 100m, 2);
        decimal patientAmount = baseAmount - coveredAmount;

        await _connection.ExecuteAsync(
            AppointmentRepositorySql.InsertAppointmentPayment,
            new
            {
                AppointmentId = appointmentId,
                InsuranceCode = insurance?.InsuranceCode,
                InsuranceName = insurance?.InsuranceName,
                BaseAmount = baseAmount,
                CoveragePercentage = coveragePercentage,
                CoveredAmount = coveredAmount,
                PatientAmount = patientAmount,
                RecordedByUserCode = recordedByUserCode
            },
            transaction);

        return Result<Unit, AppointmentRepositoryError>.Success(Unit.Value);
    }

    // Internal projection for reading the availability slot during insert.
    private sealed class SlotRow
    {
        public required int DoctorAvailabilityId { get; init; }
        public required int DoctorId { get; init; }
        public required DateOnly Date { get; init; }
        public required TimeOnly StartTime { get; init; }
        public required bool IsBooked { get; init; }
        public DateTime? DeletedAt { get; init; }
    }

    // Internal projection for the cancel transaction.
    private sealed class CancellableRow
    {
        public required int AppointmentId { get; init; }
        public required int DoctorAvailabilityId { get; init; }
    }

    // Internal projection for the newly-inserted appointment's identity.
    private sealed class NewAppointmentIdentity
    {
        public required int AppointmentId { get; init; }
        public required Guid AppointmentCode { get; init; }
    }

    // Internal projection for resolving price/AppointmentId when a payment is resolved post-booking.
    private sealed class PricingContextRow
    {
        public required int AppointmentId { get; init; }
        public decimal? BaseAmount { get; init; }
    }
}
