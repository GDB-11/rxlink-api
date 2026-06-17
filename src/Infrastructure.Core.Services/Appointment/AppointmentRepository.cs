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
        Guid consultationTypeCode)
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

            Guid newCode = await _connection.ExecuteScalarAsync<Guid>(
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

            AppointmentRow? row = await _connection.QueryFirstOrDefaultAsync<AppointmentRow>(
                AppointmentRepositorySql.GetByCode,
                new { Code = newCode },
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
        Guid code, Guid patientCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                AppointmentRepositorySql.ConfirmPayment,
                new { Code = code, PatientCode = patientCode }),
            errorFactory: AppointmentRepositoryError (ex) => new TransitionAppointmentError(ex.Message, ex));

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
        bool isPaid)
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

            Guid newCode = await _connection.ExecuteScalarAsync<Guid>(
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

            if (isPaid)
            {
                await _connection.ExecuteAsync(
                    AppointmentRepositorySql.ConfirmPaymentByAdmin,
                    new { Code = newCode },
                    transaction);
            }

            AppointmentRow? row = await _connection.QueryFirstOrDefaultAsync<AppointmentRow>(
                AppointmentRepositorySql.GetByCode,
                new { Code = newCode },
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
    public async Task<Result<int, AppointmentRepositoryError>> ConfirmPaymentByAdminAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection, AppointmentRepositorySql.ConfirmPaymentByAdmin, new { Code = code }),
            errorFactory: AppointmentRepositoryError (ex) => new AdminConfirmPaymentError(ex.Message, ex));

    /// <inheritdoc/>
    public async Task<Result<int, AppointmentRepositoryError>> RevertPaymentAsync(Guid code) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection, AppointmentRepositorySql.RevertPayment, new { Code = code }),
            errorFactory: AppointmentRepositoryError (ex) => new RevertPaymentError(ex.Message, ex));

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
}