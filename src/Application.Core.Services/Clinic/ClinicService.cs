using Application.Core.DTOs.Clinic.Errors;
using Application.Core.DTOs.Clinic.Request;
using Application.Core.DTOs.Clinic.Response;
using Application.Core.Interfaces.Clinic;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Clinic;
using Infrastructure.Core.Models.Clinic;

namespace Application.Core.Services.Clinic;

public sealed class ClinicService : IClinic
{
    private readonly IClinicRepository _repository;

    public ClinicService(IClinicRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<ClinicResponse, ClinicError>> GetDefaultAsync() =>
        _repository.GetDefaultAsync()
            .MapErrorAsync(ClinicError (error) =>
                new ClinicDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new ClinicNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<ClinicResponse, ClinicError>> UpdateDefaultAsync(UpdateClinicRequest request) =>
        _repository.UpdateDefaultAsync(
                Normalize(request.Address),
                Normalize(request.District),
                Normalize(request.Province),
                Normalize(request.Department),
                Normalize(request.Phone),
                Normalize(request.Email),
                Normalize(request.Website),
                Normalize(request.FacilityRegistrationCode))
            .MapErrorAsync(ClinicError (error) =>
                new ClinicDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new ClinicNotFoundError())
            .MapAsync(MapToResponse);

    /// <summary>Trims the value; blank becomes <c>null</c> so the column is cleared.</summary>
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClinicResponse MapToResponse(ClinicRow row) =>
        new()
        {
            ClinicCode = row.ClinicCode,
            Name = row.Name,
            Address = row.Address,
            District = row.District,
            Province = row.Province,
            Department = row.Department,
            Phone = row.Phone,
            Email = row.Email,
            Website = row.Website,
            FacilityRegistrationCode = row.FacilityRegistrationCode,
            UpdatedAt = row.UpdatedAt,
        };
}
