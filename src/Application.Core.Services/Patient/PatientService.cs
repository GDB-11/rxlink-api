using System.Text.Json;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Patient.Request;
using Application.Core.DTOs.Patient.Response;
using Application.Core.Interfaces.Patient;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Models.Patient;

namespace Application.Core.Services.Patient;

public sealed class PatientService : IPatient
{
    private readonly IPatientRepository _repository;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public PatientService(IPatientRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<PatientPageResponse, PatientError>> GetPageAsync(PatientPageRequest request) =>
        _repository.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, request.Search)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> CreateAsync(CreatePatientRequest request)
    {
        string allergiesJson = JsonSerializer.Serialize(
            request.Allergies.Select(a => new
                { AllergyCode = a.AllergyCode, SeverityCode = a.SeverityCode, Notes = a.Notes }));

        return _repository.InsertAsync(request.PersonCode, allergiesJson)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientPersonNotFoundError())
            .MapAsync(MapToResponse);
    }

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> UpdateAsync(Guid code, UpdatePatientRequest request) =>
        _repository.UpdateAsync(code, request.MedicalRecordNumber)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> GetSelfAsync(Guid patientCode) =>
        _repository.GetByCodeAsync(patientCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> UpdateSelfAsync(Guid patientCode, UpdatePatientSelfRequest request) =>
        _repository.UpdatePersonContactAsync(
                patientCode,
                request.Phone,
                request.AlternativePhone,
                request.Address,
                request.EmergencyContactName,
                request.EmergencyContactPhone)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<PatientAllergyResponse, PatientError>> AddAllergyAsync(
        Guid patientCode, PatientAllergyRequest request) =>
        _repository.AddAllergyAsync(patientCode, request.AllergyCode, request.SeverityCode, request.Notes)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .MapAsync(MapAllergyToResponse);

    /// <inheritdoc/>
    public Task<Result<PatientAllergyResponse, PatientError>> UpdateAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, PatientAllergyRequest request) =>
        _repository.UpdateAllergyAsync(patientCode, patientAllergyCode, request.SeverityCode, request.Notes)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientAllergyNotFoundError())
            .MapAsync(MapAllergyToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> RemoveAllergyAsync(
        Guid patientCode, Guid patientAllergyCode, Guid performedByUserCode) =>
        _repository.DeleteAllergyAsync(patientCode, patientAllergyCode, performedByUserCode)
            .MapErrorAsync(PatientError (error) =>
                new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientAllergyNotFoundError())
            .MapAsync(_ => Unit.Value);

    private static PatientPageResponse BuildPageResponse(IEnumerable<PatientRow> rows, int page, int pageSize)
    {
        List<PatientRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PatientPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static PatientAllergyResponse MapAllergyToResponse(PatientAllergyRow row) =>
        new()
        {
            PatientAllergyCode = row.PatientAllergyCode,
            AllergyCode = row.AllergyCode,
            AllergyName = row.AllergyName,
            SeverityCode = row.SeverityCode,
            SeverityName = row.SeverityName,
            Notes = row.Notes
        };

    private static PatientResponse MapToResponse(PatientRow row) =>
        new()
        {
            PatientCode = row.PatientCode,
            PersonCode = row.PersonCode,
            MedicalRecordNumber = row.MedicalRecordNumber,
            IsActive = row.IsActive,
            Names = row.Names,
            Surnames = row.Surnames,
            BirthDate = row.BirthDate,
            Phone = row.Phone,
            AlternativePhone = row.AlternativePhone,
            Email = row.Email,
            Address = row.Address,
            EmergencyContactName = row.EmergencyContactName,
            EmergencyContactPhone = row.EmergencyContactPhone,
            Allergies = JsonSerializer.Deserialize<List<PatientAllergyResponse>>(
                row.AllergiesJson, JsonOptions) ?? []
        };
}