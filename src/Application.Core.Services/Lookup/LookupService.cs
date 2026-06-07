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

    /// <inheritdoc/>
    public async Task<Result<UserLookupsResponse, LookupError>> GetUserLookupsAsync()
    {
        var sexesResult = await _repository.GetSexesAsync();
        if (sexesResult.IsFailure)
            return Result<UserLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(sexesResult.Error!.Message, sexesResult.Error.Details, sexesResult.Error.Exception));

        var docTypesResult = await _repository.GetActiveDocumentTypesAsync();
        if (docTypesResult.IsFailure)
            return Result<UserLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(docTypesResult.Error!.Message, docTypesResult.Error.Details, docTypesResult.Error.Exception));

        var rolesResult = await _repository.GetActiveRolesAsync();
        if (rolesResult.IsFailure)
            return Result<UserLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(rolesResult.Error!.Message, rolesResult.Error.Details, rolesResult.Error.Exception));

        var specialtiesResult = await _repository.GetActiveSpecialtiesAsync();
        if (specialtiesResult.IsFailure)
            return Result<UserLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(specialtiesResult.Error!.Message, specialtiesResult.Error.Details, specialtiesResult.Error.Exception));

        return Result<UserLookupsResponse, LookupError>.Success(new UserLookupsResponse
        {
            Sexes         = [.. sexesResult.Value!.Select(ToGuidItem)],
            DocumentTypes = [.. docTypesResult.Value!.Select(ToGuidItem)],
            Roles         = [.. rolesResult.Value!.Select(ToGuidItem)],
            Specialties   = [.. specialtiesResult.Value!.Select(ToGuidItem)]
        });
    }

    /// <inheritdoc/>
    public async Task<Result<PatientLookupsResponse, LookupError>> GetPatientLookupsAsync()
    {
        var severitiesResult = await _repository.GetAllergySeveritiesAsync();
        if (severitiesResult.IsFailure)
            return Result<PatientLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(severitiesResult.Error!.Message, severitiesResult.Error.Details, severitiesResult.Error.Exception));

        return Result<PatientLookupsResponse, LookupError>.Success(new PatientLookupsResponse
        {
            AllergySeverities = [.. severitiesResult.Value!.Select(ToGuidItem)]
        });
    }

    /// <inheritdoc/>
    public async Task<Result<PrescriptionLookupsResponse, LookupError>> GetPrescriptionLookupsAsync()
    {
        var statusesResult = await _repository.GetActivePrescriptionStatusesAsync();
        if (statusesResult.IsFailure)
            return Result<PrescriptionLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(statusesResult.Error!.Message, statusesResult.Error.Details, statusesResult.Error.Exception));

        var medicationsResult = await _repository.GetActiveMedicationsAsync();
        if (medicationsResult.IsFailure)
            return Result<PrescriptionLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(medicationsResult.Error!.Message, medicationsResult.Error.Details, medicationsResult.Error.Exception));

        var routesResult = await _repository.GetActiveAdministrationRoutesAsync();
        if (routesResult.IsFailure)
            return Result<PrescriptionLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(routesResult.Error!.Message, routesResult.Error.Details, routesResult.Error.Exception));

        var frequenciesResult = await _repository.GetActiveFrequenciesAsync();
        if (frequenciesResult.IsFailure)
            return Result<PrescriptionLookupsResponse, LookupError>.Failure(
                new LookupDataAccessError(frequenciesResult.Error!.Message, frequenciesResult.Error.Details, frequenciesResult.Error.Exception));

        return Result<PrescriptionLookupsResponse, LookupError>.Success(new PrescriptionLookupsResponse
        {
            PrescriptionStatuses = [.. statusesResult.Value!.Select(ToGuidItem)],
            Medications          = [.. medicationsResult.Value!.Select(ToGuidItem)],
            AdministrationRoutes = [.. routesResult.Value!.Select(ToGuidItem)],
            Frequencies          = [.. frequenciesResult.Value!.Select(ToGuidItem)]
        });
    }

    private static LookupItemResponse ToItem(LookupRow row) =>
        new() { Id = row.Id, Name = row.Name };

    private static GuidLookupItemResponse ToGuidItem(GuidLookupRow row) =>
        new() { Code = row.Code, Name = row.Name };
}
