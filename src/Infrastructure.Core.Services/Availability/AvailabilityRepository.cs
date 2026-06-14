using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Availability;
using Infrastructure.Core.Interfaces.Availability;
using Infrastructure.Core.Models.Availability;

namespace Infrastructure.Core.Services.Availability;

public sealed class AvailabilityRepository : BaseDatabaseService, IAvailabilityRepository
{
    private readonly IDbConnection _connection;

    public AvailabilityRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public async Task<Result<int?, AvailabilityRepositoryError>> GetDoctorUserIdAsync(Guid doctorCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, int?>(
                _connection,
                AvailabilityRepositorySql.GetDoctorUserId,
                new { DoctorCode = doctorCode }),
            errorFactory: AvailabilityRepositoryError (ex) => new GetDoctorUserIdError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<AvailabilityRow?, AvailabilityRepositoryError>> InsertOneAsync(
        int doctorUserId, DateTime date, TimeOnly startTime, Guid createdByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, AvailabilityRow>(
                _connection,
                AvailabilityRepositorySql.InsertOne,
                new
                {
                    DoctorUserId = doctorUserId, Date = date, StartTime = startTime,
                    CreatedByUserCode = createdByUserCode
                }),
            errorFactory: AvailabilityRepositoryError (ex) => new InsertAvailabilityError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AvailabilityRow>, AvailabilityRepositoryError>> GetByDoctorAndMonthAsync(
        Guid doctorCode, DateTime startDate, DateTime endDate) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, AvailabilityRow>(
                _connection,
                AvailabilityRepositorySql.GetByDoctorAndMonth,
                new { DoctorCode = doctorCode, StartDate = startDate, EndDate = endDate }),
            errorFactory: AvailabilityRepositoryError (ex) => new GetAvailabilityError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<bool?, AvailabilityRepositoryError>> GetIsBookedAsync(Guid availabilityCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteScalarAsync<object, bool?>(
                _connection,
                AvailabilityRepositorySql.GetIsBooked,
                new { Code = availabilityCode }),
            errorFactory: AvailabilityRepositoryError (ex) => new GetIsBookedError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<int, AvailabilityRepositoryError>> SoftDeleteAsync(
        Guid availabilityCode, Guid deletedByUserCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteNonQueryAsync(
                _connection,
                AvailabilityRepositorySql.SoftDelete,
                new { Code = availabilityCode, DeletedByUserCode = deletedByUserCode }),
            errorFactory: AvailabilityRepositoryError (ex) => new SoftDeleteAvailabilityError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AvailableDateRow>, AvailabilityRepositoryError>> GetAvailableDatesAsync(
        Guid doctorCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, AvailableDateRow>(
                _connection,
                AvailabilityRepositorySql.GetAvailableDates,
                new { DoctorCode = doctorCode }),
            errorFactory: AvailabilityRepositoryError (ex) => new GetAvailableDatesError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AvailableSlotRow>, AvailabilityRepositoryError>> GetAvailableSlotsAsync(
        Guid doctorCode, DateTime date) =>
        await Result.TryAsync(
            operation: async () => await ExecuteQueryAsync<object, AvailableSlotRow>(
                _connection,
                AvailabilityRepositorySql.GetAvailableSlots,
                new { DoctorCode = doctorCode, Date = date }),
            errorFactory: AvailabilityRepositoryError (ex) => new GetAvailableSlotsError(ex.Message, ex)
        );
}