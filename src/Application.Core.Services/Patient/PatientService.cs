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

    public PatientService(IPatientRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<PatientPageResponse, PatientError>> GetPageAsync(PatientPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _repository.GetPageAsync(offset, request.PageSize, request.Search)
            .MapErrorAsync(PatientError (error) => new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));
    }

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> CreateAsync(CreatePatientRequest request) =>
        _repository.InsertAsync(
            request.Names, request.Surnames, request.BirthDate,
            request.Phone, request.AlternativePhone, request.Email,
            request.Address, request.EmergencyContactName, request.EmergencyContactPhone,
            request.MedicalRecordNumber)
            .MapErrorAsync(PatientError (error) => new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientDataAccessError("No se pudo registrar el paciente."))
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<PatientResponse, PatientError>> UpdateAsync(Guid code, UpdatePatientRequest request) =>
        _repository.UpdateAsync(
            code, request.Names, request.Surnames, request.BirthDate,
            request.Phone, request.AlternativePhone, request.Email,
            request.Address, request.EmergencyContactName, request.EmergencyContactPhone,
            request.MedicalRecordNumber)
            .MapErrorAsync(PatientError (error) => new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new PatientNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(PatientError (error) => new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, PatientError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(PatientError (error) => new PatientDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new PatientNotFoundError())
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

    private static PatientResponse MapToResponse(PatientRow row) =>
        new()
        {
            PatientCode = row.PatientCode,
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
            EmergencyContactPhone = row.EmergencyContactPhone
        };
}
