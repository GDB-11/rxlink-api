using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Lookup;
using BindSharp;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Models.Lookup;

namespace Application.Core.Services.Lookup;

public sealed class LookupService : ILookup
{
    private readonly ILookupRepository _repository;

    public LookupService(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<MedicationLookupsResponse, LookupError>> GetMedicationLookupsAsync() =>
        _repository.GetPharmaceuticalFormsAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .BindAsync(forms =>
                _repository.GetAdministrationRoutesAsync()
                    .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                    .MapAsync(routes => new MedicationLookupsResponse
                    {
                        PharmaceuticalForms = [.. forms.Select(ToItem)],
                        AdministrationRoutes = [.. routes.Select(ToItem)]
                    }));

    /// <inheritdoc/>
    public Task<Result<UserLookupsResponse, LookupError>> GetUserLookupsAsync() =>
        _repository.GetSexesAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .BindAsync(sexes => _repository.GetActiveDocumentTypesAsync()
                .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                .BindAsync(docTypes => _repository.GetActiveRolesAsync()
                    .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                    .BindAsync(roles => _repository.GetActiveSpecialtiesAsync()
                        .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                        .MapAsync(specialties => new UserLookupsResponse
                        {
                            Sexes = [.. sexes.Select(ToGuidItem)],
                            DocumentTypes = [.. docTypes.Select(ToGuidItem)],
                            Roles = [.. roles.Select(ToGuidItem)],
                            Specialties = [.. specialties.Select(ToGuidItem)]
                        }))));

    /// <inheritdoc/>
    public Task<Result<PatientLookupsResponse, LookupError>> GetPatientLookupsAsync() =>
        _repository.GetAllergySeveritiesAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(severities => new PatientLookupsResponse
            {
                AllergySeverities = [.. severities.Select(ToGuidItem)]
            });

    /// <inheritdoc/>
    public Task<Result<PrescriptionLookupsResponse, LookupError>> GetPrescriptionLookupsAsync() =>
        _repository.GetActivePrescriptionStatusesAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .BindAsync(statuses => _repository.GetActiveMedicationsAsync()
                .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                .BindAsync(medications => _repository.GetActiveAdministrationRoutesAsync()
                    .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                    .BindAsync(routes => _repository.GetActiveFrequenciesAsync()
                        .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
                        .MapAsync(frequencies => new PrescriptionLookupsResponse
                        {
                            PrescriptionStatuses = [.. statuses.Select(ToGuidItem)],
                            Medications = [.. medications.Select(ToMedicationItem)],
                            AdministrationRoutes = [.. routes.Select(ToGuidItem)],
                            Frequencies = [.. frequencies.Select(ToGuidItem)]
                        }))));

    /// <inheritdoc/>
    public Task<Result<AppointmentLookupsResponse, LookupError>> GetAppointmentLookupsAsync() =>
        _repository.GetActiveConsultationTypesAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(types => new AppointmentLookupsResponse
            {
                ConsultationTypes = [.. types.Select(ToGuidItem)]
            });

    private static LookupItemResponse ToItem(LookupRow row) =>
        new() { Id = row.Id, Name = row.Name };

    private static GuidLookupItemResponse ToGuidItem(GuidLookupRow row) =>
        new() { Code = row.Code, Name = row.Name };

    private static MedicationLookupItemResponse ToMedicationItem(MedicationLookupRow row) =>
        new()
        {
            Code = row.Code, Name = row.Name, DefaultDose = row.DefaultDose,
            DefaultAdministrationRouteCode = row.DefaultAdministrationRouteCode
        };
}