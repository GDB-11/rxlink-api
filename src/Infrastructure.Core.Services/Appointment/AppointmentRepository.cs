using System.Data;
using BindSharp;
using Dapper;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Models.Appointment;

namespace Infrastructure.Core.Services.Appointment;

public sealed class AppointmentRepository : BaseDatabaseService, IAppointmentRepository
{
    private readonly IDbConnection _connection;

    public AppointmentRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<AppointmentRow?, AppointmentRepositoryError>> InsertAsync(
        Guid patientCode,
        Guid availabilityCode,
        Guid consultationTypeCode,
        bool payNow,
        Guid? insuranceCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
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
                    identity.AppointmentId, slot.DoctorId, consultationTypeId.Value, insuranceCode,
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
        Guid code, Guid patientCode, Guid? insuranceCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            PricingContextRow? pricing = await _connection.QueryFirstOrDefaultAsync<PricingContextRow>(
                AppointmentRepositorySql.GetAppointmentPricingContext,
                new { Code = code },
                transaction);

            if (pricing is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            Result<Unit, AppointmentRepositoryError> paymentResult = await ResolveAndRecordPaymentAsync(
                pricing.AppointmentId, pricing.BaseAmount, insuranceCode, recordedByUserCode: null, transaction);

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
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                AppointmentRepositorySql.Complete,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
            errorFactory: AppointmentRepositoryError (ex) => new TransitionAppointmentError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> NoShowAsync(
        Guid code, Guid performedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                AppointmentRepositorySql.NoShow,
                new { Code = code, PerformedByUserCode = performedByUserCode }),
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
        Guid? insuranceCode,
        Guid? recordedByUserCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
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
                    identity.AppointmentId, slot.DoctorId, consultationTypeId.Value, insuranceCode,
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
        Guid code, Guid? insuranceCode, Guid recordedByUserCode)
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using IDbTransaction transaction = _connection.BeginTransaction();

        try
        {
            PricingContextRow? pricing = await _connection.QueryFirstOrDefaultAsync<PricingContextRow>(
                AppointmentRepositorySql.GetAppointmentPricingContext,
                new { Code = code },
                transaction);

            if (pricing is null)
            {
                transaction.Rollback();
                return Result<int, AppointmentRepositoryError>.Success(0);
            }

            Result<Unit, AppointmentRepositoryError> paymentResult = await ResolveAndRecordPaymentAsync(
                pricing.AppointmentId, pricing.BaseAmount, insuranceCode, recordedByUserCode, transaction);

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
    /// Resolves the base price for a doctor/consultation-type pair, resolves the insurance (if
    /// any), and inserts the AppointmentPayment snapshot. Returns a typed failure when
    /// <paramref name="insuranceCode"/> does not match an active insurance — nothing is written
    /// in that case.
    /// </summary>
    private async Task<Result<Unit, AppointmentRepositoryError>> ResolveAndRecordPaymentAsync(
        int appointmentId, int doctorId, int consultationTypeId, Guid? insuranceCode,
        Guid? recordedByUserCode, IDbTransaction transaction)
    {
        decimal baseAmount = await _connection.ExecuteScalarAsync<decimal>(
            AppointmentRepositorySql.GetBasePriceForBooking,
            new { DoctorId = doctorId, ConsultationTypeId = consultationTypeId },
            transaction);

        return await ResolveAndRecordPaymentAsync(
            appointmentId, baseAmount, insuranceCode, recordedByUserCode, transaction);
    }

    /// <summary>
    /// Resolves the insurance (if any) for an already-known base price and inserts the
    /// AppointmentPayment snapshot. Returns a typed failure when <paramref name="insuranceCode"/>
    /// does not match an active insurance — nothing is written in that case.
    /// </summary>
    private async Task<Result<Unit, AppointmentRepositoryError>> ResolveAndRecordPaymentAsync(
        int appointmentId, decimal baseAmount, Guid? insuranceCode,
        Guid? recordedByUserCode, IDbTransaction transaction)
    {
        int? insuranceId = null;
        decimal coveragePercentage = 0m;

        if (insuranceCode.HasValue)
        {
            InsuranceForPaymentRow? insurance = await _connection.QueryFirstOrDefaultAsync<InsuranceForPaymentRow>(
                AppointmentRepositorySql.GetInsuranceForPayment,
                new { InsuranceCode = insuranceCode.Value },
                transaction);

            if (insurance is null)
                return Result<Unit, AppointmentRepositoryError>.Failure(new InsertInsuranceNotFoundError());

            insuranceId = insurance.InsuranceId;
            coveragePercentage = insurance.CoveragePercentage;
        }

        decimal coveredAmount = Math.Round(baseAmount * coveragePercentage / 100m, 2);
        decimal patientAmount = baseAmount - coveredAmount;

        await _connection.ExecuteAsync(
            AppointmentRepositorySql.InsertAppointmentPayment,
            new
            {
                AppointmentId = appointmentId,
                InsuranceId = insuranceId,
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
        public required decimal BaseAmount { get; init; }
    }

    // Internal projection for an active insurance's coverage.
    private sealed class InsuranceForPaymentRow
    {
        public required int InsuranceId { get; init; }
        public required decimal CoveragePercentage { get; init; }
    }
}