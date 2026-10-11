using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Response;
using Application.Core.Interfaces.Specialty;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Models.Specialty;

namespace Application.Core.Services.Specialty;

public sealed class SpecialtyService : ISpecialty
{
    private readonly ISpecialtyRepository _repository;

    public SpecialtyService(ISpecialtyRepository repository)
    {
        _repository = repository;
    }


    /// <inheritdoc/>
    public Task<Result<IEnumerable<SpecialtyWithDoctorCountResponse>, SpecialtyError>>
        GetAllActiveWithDoctorCountAsync() =>
        _repository.GetAllActiveWithDoctorCountAsync()
            .MapErrorAsync(SpecialtyError (error) =>
                new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => rows.Select(MapToPublicSpecialtyResponse));

    /// <inheritdoc/>
    public Task<Result<IEnumerable<DoctorSummaryResponse>, SpecialtyError>> GetDoctorsBySpecialtyCodeAsync(
        Guid specialtyCode, string? search = null) =>
        _repository.GetDoctorsBySpecialtyCodeAsync(specialtyCode, search)
            .MapErrorAsync(SpecialtyError (error) =>
                new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new SpecialtyNotFoundError())
            .MapAsync(rows => rows.Select(MapToDoctorSummaryResponse));

    private static SpecialtyWithDoctorCountResponse MapToPublicSpecialtyResponse(SpecialtyWithDoctorCountRow row) =>
        new()
        {
            SpecialtyCode = row.SpecialtyCode,
            Name = row.Name,
            DoctorCount = row.DoctorCount
        };

    private static DoctorSummaryResponse MapToDoctorSummaryResponse(DoctorSummaryRow row) =>
        new()
        {
            UserCode = row.UserCode!.Value,
            Names = row.Names!,
            Surnames = row.Surnames!,
            LicenseNumber = row.LicenseNumber,
            SpecialtyName = row.SpecialtyName
        };
}