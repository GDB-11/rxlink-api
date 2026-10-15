using System.Text.Json;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Patient.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Prescription.Response;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Interfaces.Prescription;
using Application.Core.Services.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Models.Patient;
using Infrastructure.Core.Models.Prescription;

namespace Application.Core.Services.Prescription;

public sealed class PrescriptionSafetyService : IPrescriptionSafety
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IPatientRepository _patients;
    private readonly ICatalogSnapshotStore _catalog;
    private readonly IPatientAccessPolicy _accessPolicy;
    private readonly IRecordAccessLogger _accessLogger;
    private readonly IPatientAllergyResolver _allergyResolver;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PrescriptionSafetyService(IPrescriptionRepository prescriptions, IPatientRepository patients,
        ICatalogSnapshotStore catalog, IPatientAccessPolicy accessPolicy, IRecordAccessLogger accessLogger,
        IPatientAllergyResolver allergyResolver)
    {
        _allergyResolver = allergyResolver;
        _prescriptions = prescriptions;
        _patients = patients;
        _catalog = catalog;
        _accessPolicy = accessPolicy;
        _accessLogger = accessLogger;
    }

    /// <inheritdoc/>
    public async Task<Result<PrescriptionSafetyCheckResponse, PrescriptionError>> CheckAsync(Guid prescriptionCode)
    {
        // Without the rules a "no warnings" answer would be a false all-clear, so fail instead.
        Infrastructure.Core.Interfaces.Catalog.CatalogSnapshot? snapshot = _catalog.Current;
        if (snapshot is null)
            return new PrescriptionCatalogValidationError(new CatalogValidationUnavailableError());

        Result<PrescriptionRow?, Infrastructure.Core.DTOs.Prescription.PrescriptionRepositoryError> prescription =
            await _prescriptions.GetByCodeAsync(prescriptionCode);
        if (prescription.IsFailure)
            return Failure(prescription.Error);
        if (prescription.Value is null)
            return new PrescriptionNotFoundError();
        PrescriptionRow row = prescription.Value;

        Result<PatientAccessDecision, PrescriptionError> access = await _accessPolicy
            .RequireAsync<PrescriptionError>(row.PatientCode, PatientDataScope.Clinical,
                error => new PrescriptionDataAccessError(error.Message, error.Details, error.Exception),
                new PrescriptionPatientForbiddenError());
        if (access.IsFailure)
            return access.Error;

        var patient = await _patients.GetByCodeAsync(row.PatientCode);
        if (patient.IsFailure)
            return new PrescriptionDataAccessError(patient.Error.Message, patient.Error.Details, patient.Error.Exception);
        if (patient.Value is null)
            return new PrescriptionPatientForbiddenError();

        var others = await _prescriptions.GetActiveMedicationsOfPatientAsync(row.PatientCode, prescriptionCode);
        if (others.IsFailure)
            return Failure(others.Error);

        await _accessLogger.LogAsync(AccessResourceTypes.Prescription, row.PatientCode, prescriptionCode,
            access.Value == PatientAccessDecision.AllowedViaBreakGlass);

        List<PrescriptionDetailResponse> lines =
            JsonSerializer.Deserialize<List<PrescriptionDetailResponse>>(row.DetailsJson, JsonOptions) ?? [];
        List<PatientAllergyResponse> allergies = _allergyResolver.ResolveJson(patient.Value.AllergiesJson);

        return new PrescriptionSafetyCheckResponse
        {
            Warnings = PrescriptionSafetyEvaluator.Evaluate(
                snapshot,
                lines.Select(l => l.MedicationCode).ToList(),
                allergies.Select(a => new RecordedAllergy(a.AllergyCode, a.AllergyName, a.SeverityName)).ToList(),
                others.Value.Select(o => new OtherActiveMedication(o.PrescriptionCode, o.MedicationCode)).ToList())
        };
    }

    private static PrescriptionError Failure(Infrastructure.Core.DTOs.Prescription.PrescriptionRepositoryError error) =>
        new PrescriptionDataAccessError(error.Message, error.Details, error.Exception);
}
