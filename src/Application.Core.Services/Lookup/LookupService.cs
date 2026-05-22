using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Lookup;
using BindSharp;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Models.Lookup;
using InfraError = Infrastructure.Core.DTOs.Lookup.LookupRepositoryError;

namespace Application.Core.Services.Lookup;

public sealed class LookupService : ILookup
{
    private readonly ILookupRepository _repository;

    public LookupService(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<MedicationLookupsResponse, LookupError>> GetMedicationLookupsAsync()
    {
        Result<IEnumerable<LookupRow>, InfraError> formsResult = await _repository.GetPharmaceuticalFormsAsync();

        if (formsResult.IsFailure)
            return Result<MedicationLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(formsResult.Error!.Message, formsResult.Error.Details, formsResult.Error.Exception));

        Result<IEnumerable<LookupRow>, InfraError> routesResult = await _repository.GetAdministrationRoutesAsync();

        if (routesResult.IsFailure)
            return Result<MedicationLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(routesResult.Error!.Message, routesResult.Error.Details, routesResult.Error.Exception));

        return Result<MedicationLookupsResponse, LookupError>.Success(new MedicationLookupsResponse
        {
            PharmaceuticalForms = [.. formsResult.Value!.Select(ToItem)],
            AdministrationRoutes = [.. routesResult.Value!.Select(ToItem)]
        });
    }

    private static LookupItemResponse ToItem(LookupRow row) =>
        new() { Id = row.Id, Name = row.Name };
}
