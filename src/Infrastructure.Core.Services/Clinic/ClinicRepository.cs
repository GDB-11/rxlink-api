using System.Data;
using BindSharp;
using Infrastructure.Core.DTOs.Clinic;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Clinic;
using Infrastructure.Core.Models.Clinic;

namespace Infrastructure.Core.Services.Clinic;

public sealed class ClinicRepository : BaseDatabaseService, IClinicRepository
{
    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public ClinicRepository(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<ClinicRow?, ClinicRepositoryError>> GetDefaultAsync() =>
        await Result.TryAsync(
            operation: async () => await ExecuteFirstOrDefaultAsync<object, ClinicRow>(
                _connection,
                ClinicRepositorySql.GetDefault,
                new { }),
            errorFactory: ClinicRepositoryError (ex) => new GetClinicError(ex.Message, ex)
        );

    /// <inheritdoc/>
    public async Task<Result<ClinicRow?, ClinicRepositoryError>> UpdateDefaultAsync(
        string? address, string? district, string? province, string? department,
        string? phone, string? email, string? website, string? facilityRegistrationCode) =>
        await Result.TryAsync(
            operation: async () => await ExecuteAuditedFirstOrDefaultAsync<object, ClinicRow>(_connection, _userContext,
                ClinicRepositorySql.UpdateDefault,
                new
                {
                    Address = address,
                    District = district,
                    Province = province,
                    Department = department,
                    Phone = phone,
                    Email = email,
                    Website = website,
                    FacilityRegistrationCode = facilityRegistrationCode,
                    UserCode = _userContext.UserCode,
                }),
            errorFactory: ClinicRepositoryError (ex) => new UpdateClinicError(ex.Message, ex)
        );
}
