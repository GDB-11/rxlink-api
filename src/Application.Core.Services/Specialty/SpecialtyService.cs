using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Response;
using Application.Core.Interfaces.Specialty;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;
using Infrastructure.Core.Models.Specialty;

namespace Application.Core.Services.Specialty;

/// <summary>
/// Public specialty reads. Doctors are counted by specialty code in the partner database; names and
/// availability come from the catalog snapshot merged with the partner's own enablement.
/// </summary>
public sealed class SpecialtyService : ISpecialty
{
    private readonly ISpecialtyRepository _repository;
    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerCatalogRepository _partner;

    public SpecialtyService(ISpecialtyRepository repository, ICatalogSnapshotStore store, IPartnerCatalogRepository partner)
    {
        _repository = repository;
        _store = store;
        _partner = partner;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<SpecialtyWithDoctorCountResponse>, SpecialtyError>>
        GetAllActiveWithDoctorCountAsync()
    {
        if (_store.Current is not { } catalog) return Failure<IEnumerable<SpecialtyWithDoctorCountResponse>>(new SpecialtyCatalogUnavailableError());

        var enabled = await EnabledCodesAsync();
        if (!enabled.IsSuccess) return Failure<IEnumerable<SpecialtyWithDoctorCountResponse>>(enabled.Error);

        var counts = await _repository.GetDoctorCountsAsync();
        if (!counts.IsSuccess) return Failure<IEnumerable<SpecialtyWithDoctorCountResponse>>(DataAccess(counts.Error));
        Dictionary<Guid, int> byCode = counts.Value.ToDictionary(r => r.SpecialtyCode, r => r.DoctorCount);

        IEnumerable<SpecialtyWithDoctorCountResponse> items = catalog.Specialties.Values
            .Where(s => s.IsActive && enabled.Value.Contains(s.Code))
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Code)
            .Select(s => new SpecialtyWithDoctorCountResponse
            {
                SpecialtyCode = s.Code,
                Name = s.Name,
                DoctorCount = byCode.GetValueOrDefault(s.Code)
            })
            .ToList();

        return Result<IEnumerable<SpecialtyWithDoctorCountResponse>, SpecialtyError>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<DoctorSummaryResponse>, SpecialtyError>> GetDoctorsBySpecialtyCodeAsync(
        Guid specialtyCode, string? search = null)
    {
        if (_store.Current is not { } catalog) return Failure<IEnumerable<DoctorSummaryResponse>>(new SpecialtyCatalogUnavailableError());

        var enabled = await EnabledCodesAsync();
        if (!enabled.IsSuccess) return Failure<IEnumerable<DoctorSummaryResponse>>(enabled.Error);

        if (!catalog.Specialties.TryGetValue(specialtyCode, out SpecialtyCatalogRow? specialty)
            || !specialty.IsActive
            || !enabled.Value.Contains(specialtyCode))
            return Failure<IEnumerable<DoctorSummaryResponse>>(new SpecialtyNotFoundError());

        var doctors = await _repository.GetDoctorsBySpecialtyCodeAsync(specialtyCode, search);
        if (!doctors.IsSuccess) return Failure<IEnumerable<DoctorSummaryResponse>>(DataAccess(doctors.Error));

        IEnumerable<DoctorSummaryResponse> items = doctors.Value
            .Select(d => new DoctorSummaryResponse
            {
                UserCode = d.UserCode,
                Names = d.Names,
                Surnames = d.Surnames,
                LicenseNumber = d.LicenseNumber,
                SpecialtyName = specialty.Name
            })
            .ToList();

        return Result<IEnumerable<DoctorSummaryResponse>, SpecialtyError>.Success(items);
    }

    private async Task<Result<HashSet<Guid>, SpecialtyError>> EnabledCodesAsync()
    {
        var settings = await _partner.GetAllSpecialtiesAsync();
        return settings.IsSuccess
            ? Result<HashSet<Guid>, SpecialtyError>.Success(
                settings.Value.Where(r => r.IsEnabled).Select(r => r.SpecialtyCode).ToHashSet())
            : Result<HashSet<Guid>, SpecialtyError>.Failure(
                new SpecialtyDataAccessError(settings.Error.Message, settings.Error.Details, settings.Error.Exception));
    }

    private static SpecialtyError DataAccess(Infrastructure.Core.DTOs.Specialty.SpecialtyRepositoryError error) =>
        new SpecialtyDataAccessError(error.Message, error.Details, error.Exception);

    private static Result<T, SpecialtyError> Failure<T>(SpecialtyError error) =>
        Result<T, SpecialtyError>.Failure(error);
}
